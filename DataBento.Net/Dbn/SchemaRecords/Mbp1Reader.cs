using DataBento.Net.Utils;

namespace DataBento.Net.Dbn.SchemaRecords;

public class Mbp1Reader
{
    private Mbp1Struct _state;
    public void Initialize(ReadOnlySpan<byte> data)
    {
        _state = Mbp1Struct.UnsafeReference(data);
    }
    public ulong TsEvent => _state.TsEvent;
    public ulong TsRecv => _state.TsRecv;
    public uint InstrumentId => _state.InstrumentId;
    public decimal PriceDecimal => _state.PriceDecimal;
    public decimal BidDecimal => _state.BidDecimal;
    public decimal AskDecimal => _state.AskDecimal;
    public uint BidSize => _state.BidSz00;
    public uint AskSize => _state.AskSz00;
    public RecordAction RecordAction => _state.RecordAction;
    public RecordSide RecordSide => _state.RecordSide;
    
    public override string ToString()
    {
        return $"Mbp1 InstrumentId={InstrumentId} TsEvent={TimestampUtils.UnixNanoToDateTime(TsEvent):O} " +
               $"{RecordAction} {RecordSide}={PriceDecimal} " +
               $"{BidDecimal} ({BidSize}) / {AskDecimal} ({AskSize})";
    }
}