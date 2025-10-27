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

    public DataBentoClient(IOptions<DataBentoConfig> config, IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;
    }
    private BentoTcpClient CreateTcpClient(string dataset, ISubscriptionMsgHandler subscriptionMsgHandler)
    {
        var config = new DataBentoTcpConfig()
        {
            Dataset = dataset,
            ApiKey = _config.Value.ApiKey,
            CompressionMode = _config.Value.CompressionMode,
            TsOut = _config.Value.TsOut
        };
        return ActivatorUtilities.CreateInstance<BentoTcpClient>(_serviceProvider, subscriptionMsgHandler, config);
    }
    public async Task Subscribe(string dataset, IEnumerable<SubscriptionKey> keys, ISubscriptionMsgHandler handler, CancellationToken cancellationToken=default)
    {
        await _locker.WaitAsync(cancellationToken);
        try
        {
            if (!_clients.TryGetValue(dataset, out var client))
            {
                client = CreateTcpClient(dataset, handler);
                _clients[dataset] = client;
                await client.StartAsync(cancellationToken);
            }

            await client.Subscribe(keys, cancellationToken);
        }
        finally
        {
            _locker.Release();
        }
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