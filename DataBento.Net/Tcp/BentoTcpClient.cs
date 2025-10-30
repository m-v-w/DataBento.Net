using System.Net.Sockets;
using DataBento.Net.Dbn;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp.ControlMessages;
using DataBento.Net.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZstdSharp;

namespace DataBento.Net.Tcp;

public class BentoTcpClient : BackgroundService, ISystemMsgHandler
{
    private const string UrlSuffix = ".lsg.databento.com";
    private const int Port = 13000;
    
    public bool Streaming => _reader?.Streaming ?? false;
    public bool Connected => _controlMsgClient?.Connected ?? false;
    private DbnStreamReader? _reader;
    private ControlMsgClient? _controlMsgClient;
    private Timer? _connectionTimeoutTimer;
    private long _lastMsgSeq = -1;
    private DateTime _lastMsgTime;
    
    private readonly string _host;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ISubscriptionMsgHandler _subscriptionMsgHandler;
    private readonly DataBentoTcpConfig _config;
    private readonly ControlMsgHandler _controlMsgHandler;
    private readonly SubscriptionHandler _subscriptionHandler;
    private readonly AsyncManualResetEvent _connectionEstablishedEvent = new ();
    
    public BentoTcpClient(ILoggerFactory loggerFactory, IRetryPolicy retryPolicy,
        ISubscriptionMsgHandler subscriptionMsgHandler, DataBentoTcpConfig config, ISubscriptionContainer subscriptionContainer)
    {
        _loggerFactory = loggerFactory;
        _retryPolicy = retryPolicy;
        _subscriptionMsgHandler = subscriptionMsgHandler;
        _config = config;
        _subscriptionHandler = new SubscriptionHandler(subscriptionContainer, config);
        _host = $"{config.Dataset.ToLowerInvariant().Replace('.','-')}{UrlSuffix}";
        _controlMsgHandler = new ControlMsgHandler(config, _subscriptionHandler, CreateLogger("ControlMsgHandler"));
        _logger = CreateLogger("BentoTcpClient");
    }

    public async Task Subscribe(IEnumerable<SubscriptionKey> keys, CancellationToken cancellationToken=default)
    {
        var client = _controlMsgClient;
        if (client == null)
            throw new InvalidOperationException("Not connected");
        await _subscriptionHandler.Subscribe(keys, client, cancellationToken);
    }

    public void PublishEmptyRecord()
    {
        _reader?.PublishEmptyRecord();
    }
    private ILogger CreateLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger($"{categoryName}-{_host}:{Port}");
    }

    private void PrintStats(DbnStreamReader reader)
    {
        var quantiles = reader.LatencyStatistics.Quantiles();
        if(quantiles.Length == 0)
            return;
        _logger.LogInformation("Latency Stats: {Quantiles}", string.Join(", ", 
            reader.LatencyStatistics.Quantiles().Select(x => $"{x.TotalMilliseconds}ms")));
    }
    private DbnStreamReader CreateReader(NetworkStream networkStream)
    {
        Stream stream = _config.CompressionMode switch
        {
            CompressionMode.ZStd => new DecompressionStream(networkStream),
            CompressionMode.None => networkStream,
            _ => throw new InvalidOperationException($"Unsupported compression mode {_config.CompressionMode}")
        };
        return new DbnStreamReader(stream, _subscriptionMsgHandler, this, 
            CreateLogger("StreamReader"), stream == networkStream);
    }
    public async Task WaitForConnectionAsync(CancellationToken cancellationToken=default)
    {
        while (!Connected)
        {
            await _connectionEstablishedEvent.WaitAsync(cancellationToken);
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retry = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var tcpClient = new TcpClient();
                tcpClient.NoDelay = true;
                _logger.LogInformation("Connecting...");
                await tcpClient.ConnectAsync(_host, Port, stoppingToken);
                await using var stream = tcpClient.GetStream();
                using (_controlMsgClient = new ControlMsgClient(stream, _controlMsgHandler, CreateLogger("ControlMsg")))
                {
                    _connectionEstablishedEvent.Set();
                    var spillover = await _controlMsgClient.Read(stoppingToken);
                    if(spillover.Length > 0 && _config.CompressionMode != CompressionMode.None)
                        throw new InvalidOperationException("Spillover data present with compression enabled");
                    if(!stream.Socket.Connected)
                        continue; // disconnected on control message change
                    retry = 0;
                    _lastMsgSeq = -1;
                    await using (_reader = CreateReader(stream))
                    {
                        await _reader.Write(spillover);
                        await _reader.Read(stoppingToken);
                        PrintStats(_reader);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error");
            }
            finally
            {
                _controlMsgClient = null;
                _reader = null;
                _connectionEstablishedEvent.Reset();
            }
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Connection closed"); 
                return;
            }
            var retryDelay = _retryPolicy.NextRetryDelay(retry++);
            if (retryDelay == null)
            {
                _logger.LogError("All retry attempts failed");
                return;
            }
            _logger.LogInformation("Reconnecting in {Delay}", retryDelay);
            try
            {
                await Task.Delay(retryDelay.Value, stoppingToken);
            }
            catch (TaskCanceledException) { 
                // Ignore
            }
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var interval = _config.FlushMsgInterval ?? TimeSpan.FromSeconds(1);
        _connectionTimeoutTimer = new Timer(ConnectionTimeoutTick, null, interval, interval);
        return base.StartAsync(cancellationToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if(_connectionTimeoutTimer != null)
            await _connectionTimeoutTimer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
    private void ConnectionTimeoutTick(object? o)
    {
        var reader = _reader;
        if (reader == null)
            return;
        if(_config.FlushMsgInterval.HasValue)
            PublishEmptyRecord();
        var msgSeq = reader.MsgSeq;
        if (msgSeq > _lastMsgSeq)
        {
            // Message received since last tick
            _lastMsgSeq = msgSeq;
            _lastMsgTime = DateTime.UtcNow;
            return;
        }
        if ((DateTime.UtcNow - _lastMsgTime) < _config.StaleConnectionTimeout)
            return;
        _logger.LogError("Connection stale, closing...");
        _lastMsgTime = DateTime.UtcNow;
        reader.Disconnect();
    }

    ControlMsgResult ISystemMsgHandler.Handle(SystemMessage systemMsg)
    {
        if (systemMsg.Message == "Heartbeat")
        {
            _logger.LogDebug("Heartbeat");
            return ControlMsgResult.None;
        }
        if (systemMsg.Message.StartsWith("Subscription request")
            && systemMsg.Message.EndsWith("data succeeded"))
        {
            _logger.LogDebug("Subscription ACK");
            return ControlMsgResult.None;
        }
        _logger.LogWarning("Received unknown system message: {Msg}", systemMsg);
        return ControlMsgResult.None;
    }
    ControlMsgResult ISystemMsgHandler.Handle(ErrorMessage errorMsg)
    {
        _logger.LogError("Error Message {Msg}", errorMsg);
        return ControlMsgResult.Failed;
    }
    void ISystemMsgHandler.Handle(SymbolMappingMsg symbolMappingMsg)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Symbol mapping {Mapping}", symbolMappingMsg);    
        }
        _subscriptionHandler.HandleSymbolMapping(symbolMappingMsg);
    }
}