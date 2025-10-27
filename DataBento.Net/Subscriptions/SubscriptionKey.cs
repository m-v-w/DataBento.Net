using DataBento.Net.Dbn;

namespace DataBento.Net.Subscriptions;

public record SubscriptionKey(string Dataset, SchemaId SchemaId, SymbolType SymbolType, string Symbol);
public record SymbolKey(string SymbolIn, SymbolType SymbolType, string Symbol);