using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DataBento.Net.Dbn;

namespace DataBento.Net.Subscriptions;

public class DatasetSubscriptionContainer : ISubscriptionContainer
{
    private readonly string _dataset;
    private readonly ConcurrentDictionary<SubscriptionKey,int> _keys = new();
    private readonly ConcurrentDictionary<uint, SymbolKey> _currentInstrumentIds = new();
    public IEnumerable<string> Datasets => [_dataset];
    public DatasetSubscriptionContainer(string dataset)
    {
        _dataset = dataset;
    }
    public bool Add(SubscriptionKey key)
    {
        if(key.Dataset != _dataset)
            throw new ArgumentOutOfRangeException(nameof(key.Dataset), "wrong dataset");
        return _keys.TryAdd(key, 0);
    }

    public void UpdateMapping(string dataset, SymbolMappingMsg symbolMappingMsg)
    {
        if(dataset != _dataset)
            throw new ArgumentOutOfRangeException(nameof(dataset), "wrong dataset");
        var id = symbolMappingMsg.InstrumentId;
        if(id == 0)
            throw new ArgumentOutOfRangeException(nameof(symbolMappingMsg.InstrumentId), "invalid instrument id");
        var symbolKey = new SymbolKey(symbolMappingMsg.SymbolIn, symbolMappingMsg.SymbolTypeOut, symbolMappingMsg.SymbolOut);
        _currentInstrumentIds.AddOrUpdate(id, static (_,k) => k, 
            static (_, _, k) => k, symbolKey);
    }

    

    public bool TryGetSymbol(uint instrumentId, [NotNullWhen(true)] out SymbolKey? symbolKey)
    {
        return _currentInstrumentIds.TryGetValue(instrumentId, out symbolKey);
    }

    public IEnumerable<SubscriptionKey> GetKeysForDataset(string dataset)
    {
        if(dataset != _dataset)
            throw new ArgumentOutOfRangeException(nameof(dataset), "wrong dataset");
        return _keys.Keys;
    }
}