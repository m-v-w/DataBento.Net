using System.Buffers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using DataBento.Net.Dbn;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Logging;
using ZstdSharp;

namespace DataBento.Net.Tcp;

internal class ControlMsgClient : IDisposable
{
    private const byte MsgDelimiter = (byte) '\n';
    private readonly ILogger _logger;
    private readonly NetworkStream _networkStream;
    private readonly ControlMsgHandler _controlMsgHandler;
    private readonly DataBentoTcpConfig _config;
    private readonly CancellationTokenSource _phaseCompleted = new ();
    private readonly SemaphoreSlim _sendLocker = new(1,1);
    internal ControlMsgClient(NetworkStream networkStream, ControlMsgHandler controlMsgHandler, 
        DataBentoTcpConfig config, ILogger logger)
    {
        _networkStream = networkStream;
        _controlMsgHandler = controlMsgHandler;
        _config = config;
        _logger = logger;
    }

    private void Complete()
    {
        _logger.LogInformation("Control phase completed, switching to binary protocol");
        _phaseCompleted.Cancel();
    }
    private void ProcessMsg(ReadOnlySpan<char> msg)
    {
        var result = _controlMsgHandler.Handle(msg, this);
        switch (result)
        {
            case ControlMsgResult.Failed:
                _logger.LogWarning("Disconnecting, due to failure");
                Disconnect();
                break;
            case ControlMsgResult.PhaseComplete:
                Complete();
                break;
            case ControlMsgResult.None:
                break;
            default:
                throw new InvalidProgramException("Invalid control message result");
        }
    }
    private int ProcessData(ReadOnlySpan<byte> buffer)
    {
        if (_phaseCompleted.IsCancellationRequested)
            return 0; // do not process binary phase
        var pos = 0;
        while (buffer.Length > 0)
        {
            var idx = buffer.IndexOf(MsgDelimiter);
            if (idx < 0)
                return pos;
            var msgSpan = buffer.Slice(0, idx);
            using var charOwner = SpanOwner<char>.Allocate(msgSpan.Length);
            if (!Encoding.ASCII.TryGetChars(msgSpan, charOwner.Span, out var charLen))
                throw new InvalidOperationException("Unexpected string encoding");
            var charSpan = charOwner.Span.Slice(0, charLen);
            ProcessMsg(charSpan);
            buffer = buffer.Slice(msgSpan.Length + 1); // remove new line too
            pos += msgSpan.Length + 1;
        }
        return pos;
    }

    internal async Task<byte[]> Read(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _phaseCompleted.Token);
        using var writer = new ArrayPoolBufferWriter<byte>();
        while (!cts.IsCancellationRequested)
        {
            var memory = writer.GetMemory(512);
            try
            {
                var c = await _networkStream.ReadAsync(memory, cts.Token);
                if (c == 0)
                {
                    _logger.LogInformation("Connection closed by remote");
                    return writer.WrittenSpan.ToArray();
                }
                writer.Advance(c);
            } catch (OperationCanceledException)
            {
                return writer.WrittenSpan.ToArray();
            }
            var processedIndex = ProcessData(writer.WrittenSpan);
            if (processedIndex == 0)
                continue;
            if(processedIndex == writer.WrittenCount)
            {
                writer.Clear();
                continue;
            }
            var remaining = writer.WrittenSpan.Slice(processedIndex).ToArray();
            writer.Clear();
            writer.Write(remaining);
        }
        return writer.WrittenSpan.ToArray();
    }
    public async Task Send(IControlMsg msg)
    {
        await _sendLocker.WaitAsync();
        try
        {
            using var writer = new ArrayPoolBufferWriter<byte>();
            ControlMsgSerializer.Serialize(msg, writer);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("> {Msg}", Encoding.ASCII.GetString(writer.WrittenSpan).Trim());
            }
            await _networkStream.WriteAsync(writer.WrittenMemory);
        } finally {
            _sendLocker.Release();
        }
    }
    public void Disconnect()
    {
        _networkStream.Close();
    }
    public void Dispose()
    {
        _phaseCompleted.Dispose();
    }
}