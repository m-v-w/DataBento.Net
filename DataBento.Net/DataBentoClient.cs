using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DataBento.Net;

public class DataBentoClient : IHostedService
{
    private readonly IOptions<DataBentoConfig> _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, BentoTcpClient> _clients = new();
    private readonly SemaphoreSlim _locker = new(1, 1);
    private readonly SubscriptionContainer _subscriptionContainer;

    public bool Streaming => _clients.Values.All(x => x.State == TcpClientState.Streaming);

    public DataBentoClient(IOptions<DataBentoConfig> config, IServiceProvider serviceProvider, 
        ISubscriptionContainer subscriptionContainer, SubscriptionContainer subscriptionContainer1)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _subscriptionContainer = subscriptionContainer1;
        if(subscriptionContainer.Datasets.Any())
            throw new InvalidProgramException("SubscriptionContainer is not empty, use Subscribe method to add subscriptions.");
    }
    private BentoTcpClient CreateTcpClient(string dataset, ISubscriptionMsgHandler subscriptionMsgHandler)
    {
        var config = new DataBentoTcpConfig()
        {
            Dataset = dataset,
            ApiKey = _config.Value.ApiKey,
            CompressionMode = _config.Value.CompressionMode,
            TsOut = _config.Value.TsOut,
            StaleConnectionTimeout = _config.Value.StaleConnectionTimeout,
            FlushMsgInterval = _config.Value.FlushMsgInterval
        };
        return ActivatorUtilities.CreateInstance<BentoTcpClient>(_serviceProvider, subscriptionMsgHandler, config);
    }
    private void AddSubscriptions(string dataset, IEnumerable<SubscriptionKey> keys)
    {
        foreach (var key in keys)
        {
            if(key.Dataset != dataset)
                throw new ArgumentException("All symbols must belong to the configured dataset");
            _subscriptionContainer.Add(key);
        }
    }
    public async Task<BentoTcpClient> Subscribe(string dataset, IEnumerable<SubscriptionKey> keys, ISubscriptionMsgHandler handler, CancellationToken cancellationToken=default)
    {
        await _locker.WaitAsync(cancellationToken);
        try
        {
            if (_clients.TryGetValue(dataset, out var client))
            {
                await client.Subscribe(keys, cancellationToken);
                return client;
            }
            client = CreateTcpClient(dataset, handler);
            _clients[dataset] = client;
            // just add the subscriptions to the container, tcp client will send subscribe messages after connecting
            AddSubscriptions(dataset, keys);
            await client.StartAsync(cancellationToken);
            return client;
        }
        finally
        {
            _locker.Release();
        }
    }
    public Dictionary<string, TcpClientState> GetClientStates()
    {
        return _clients.ToDictionary(x => x.Key, x => x.Value.State);
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _clients.Values.Select(c => c.StopAsync(cancellationToken));
        return Task.WhenAll(tasks);
    }
}