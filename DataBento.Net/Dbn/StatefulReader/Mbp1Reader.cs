using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn.StatefulReader;

public class Mbp1Reader
{
    private Mbp1Struct _state;
    public void Initialize(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        _state = Mbp1Struct.UnsafeReference(span);
    }
    public ulong TsEvent => _state.TsEvent;
    public ulong TsRecv => _state.TsRecv;
    public uint InstrumentId => _state.InstrumentId;
    public decimal PriceDecimal => _state.PriceDecimal;
    public decimal BidDecimal => _state.BidDecimal;
    public decimal AskDecimal => _state.AskDecimal;
    public uint BidSize => _state.BidSize;
    public uint AskSize => _state.AskSize;
    public RecordAction RecordAction => _state.RecordAction;
    public RecordSide RecordSide => _state.RecordSide;
    
    public override string ToString()
    {
        var latency = TsRecv - TsEvent;
        return $"Mbp1 InstrumentId={InstrumentId} TsEvent={TimestampUtils.UnixNanoToDateTime(TsEvent):O} " +
               $"{RecordAction} {RecordSide}={PriceDecimal} " +
               $"{BidDecimal} ({BidSize}) / {AskDecimal} ({AskSize})";
    }
}