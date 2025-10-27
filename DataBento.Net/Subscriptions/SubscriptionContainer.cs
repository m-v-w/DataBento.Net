using System.Collections.Concurrent;
using DataBento.Net.Dbn;

namespace DataBento.Net.Subscriptions;

public class SubscriptionContainer : ISubscriptionContainer
{
    private readonly ConcurrentDictionary<string, DatasetSubscriptionContainer> _containers = new();
    public IEnumerable<string> Datasets => _containers.Keys;

    public IEnumerable<SubscriptionKey> GetKeysForDataset(string dataset)
    {
        if (!_containers.TryGetValue(dataset, out var container))
            return [];
        return container.GetKeysForDataset(dataset);
    }

    public bool Add(SubscriptionKey key)
    {
        var container = GetOrAdd(key.Dataset);
        return container.Add(key);
    }

    public void UpdateMapping(string dataset, SymbolMappingMsg symbolMappingMsg)
    {
        var container = GetOrAdd(dataset);
        container.UpdateMapping(dataset, symbolMappingMsg);
    }

    public DatasetSubscriptionContainer GetOrAdd(string dataset)
    {
        return _containers.GetOrAdd(dataset, k => new DatasetSubscriptionContainer(k));
    }
}