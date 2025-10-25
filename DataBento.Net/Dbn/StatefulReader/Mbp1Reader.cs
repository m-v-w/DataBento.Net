using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn.StatefulReader;

public class Mbp1Reader
{
    private const int StructSize = 16+64;
    private Mbp1Struct _state;
    public void Initialize(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if(span.Length != StructSize)
            throw new ArgumentException($"Mbp1 struct must be {StructSize} bytes");
        _state = Unsafe.As<byte, Mbp1Struct>(ref MemoryMarshal.GetReference(span));
    }
    public uint InstrumentId => _state.Header.InstrumentId;
    public ulong TsEvent => _state.Header.TsEvent;
    public ulong TsRecv => _state.TsRecv;
    public decimal PriceDecimal => _state.Price / 1_000_000_000M;
    public override string ToString()
    {
        return $"Mbp1 InstrumentId={InstrumentId} TsEvent={TsEvent} TsRecv={TsRecv} Price={PriceDecimal}";
    }
}