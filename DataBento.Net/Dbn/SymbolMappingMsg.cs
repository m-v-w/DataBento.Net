namespace DataBento.Net.Dbn;

public record SymbolMappingMsg(
    ushort PublisherId,
    uint InstrumentId,
    SymbolType SymbolTypeIn,
    string SymbolIn,
    SymbolType SymbolTypeOut,
    string SymbolOut)
{
    internal SymbolMappingMsg(in RecordHeader header, SymbolType symbolTypeIn, string symbolIn, SymbolType symbolTypeOut, string symbolOut)
        : this(header.PublisherId, header.InstrumentId, symbolTypeIn, symbolIn, symbolTypeOut, symbolOut)
    {
    }
}