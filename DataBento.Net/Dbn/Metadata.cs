namespace DataBento.Net.Dbn;

public record Metadata(
    byte Version,
    string Dataset,
    SchemaId Schema, 
    long Start,
    long End,
    ulong Limit,
    SymbolType SymbolTypeIn,
    SymbolType SymbolTypeOut,
    bool TsOut,
    int SymbolCStrLen,
    string[] Symbols,
    string[] Partial,
    string[] NotFound,
    SymbolMapping[] Mappings
);
public record SymbolMapping(string RawSymbol, MappingInterval[] Intervals);
public record MappingInterval(DateOnly StartDate, DateOnly EndDate, string Symbol);