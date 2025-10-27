using DataBento.Net.Dbn;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp.ControlMessages;

namespace DataBento.Net.Tcp;

internal class SubscriptionHandler
{
    private const int MaxSymbolsPerSubscriptionRequest = 500;
    private readonly ISubscriptionContainer _subscriptionContainer;
    private readonly DataBentoTcpConfig _config;

    public SubscriptionHandler(ISubscriptionContainer subscriptionContainer, DataBentoTcpConfig config)
    {
        _subscriptionContainer = subscriptionContainer;
        _config = config;
    }
    
    public async Task InitSubscriptions(ControlMsgClient client, CancellationToken cancellationToken)
    {
        var keys = _subscriptionContainer.GetKeysForDataset(_config.Dataset).ToArray();
        if(keys.Any(x => x.Dataset != _config.Dataset))
            throw new ArgumentException("All symbols must belong to the configured dataset");
        await Send(keys, client, cancellationToken);
    }
    public async Task Subscribe(IEnumerable<SubscriptionKey> keys, ControlMsgClient client, CancellationToken cancellationToken)
    {
        var toSub = new List<SubscriptionKey>();
        foreach (var key in keys)
        {
            if(key.Dataset != _config.Dataset)
                throw new ArgumentException("All symbols must belong to the configured dataset");
            if (_subscriptionContainer.Add(key))
                toSub.Add(key);
        }
        await Send(toSub, client, cancellationToken);
    }

    private async Task Send(ICollection<SubscriptionKey> keys, ControlMsgClient client, CancellationToken cancellationToken, uint? id=null)
    {
        if(keys.Count == 0)
            return;
        foreach (var schemaGroup in keys.GroupBy(x => (x.SchemaId, x.SymbolType)))
        {
            var chunks = schemaGroup.Select(x => x.Symbol).Chunk(MaxSymbolsPerSubscriptionRequest).ToArray();
            for (var i = 0; i < chunks.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunk = chunks[i];
                await client.Send(new SubscriptionRequest()
                {
                    Schema = schemaGroup.Key.SchemaId,
                    SymbolTypeIn = schemaGroup.Key.SymbolType,
                    Symbols = chunk,
                    IsLast = i == chunks.Length - 1,
                    Id = id
                });
            }
        }
    }
    public void HandleSymbolMapping(SymbolMappingMsg symbolMappingMsg)
    {
        _subscriptionContainer.UpdateMapping(_config.Dataset, symbolMappingMsg);
    }
}