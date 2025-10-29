using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn.SchemaRecords;

public partial struct Mbp1Struct
{
    public uint InstrumentId => Header.InstrumentId;
    public ulong TsEvent => Header.TsEvent;
    public decimal PriceDecimal => Price / 1_000_000_000M;
    public decimal BidDecimal => BidPx00 / 1_000_000_000M;
    public decimal AskDecimal => AskPx00 / 1_000_000_000M;
    public RecordAction RecordAction => (RecordAction) Action;
    public RecordSide RecordSide => (RecordSide) Side;
    public RecordFlags RecordFlags => (RecordFlags) Flags;
}