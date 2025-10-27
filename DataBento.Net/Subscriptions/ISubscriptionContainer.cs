using System.Collections;
using DataBento.Net.Dbn;

namespace DataBento.Net.Subscriptions;

public interface ISubscriptionContainer
{
    IEnumerable<SubscriptionKey> GetKeysForDataset(string dataset);
    bool Add(SubscriptionKey key);
    void UpdateMapping(string dataset, SymbolMappingMsg symbolMappingMsg);
    IEnumerable<string> Datasets { get; }
}