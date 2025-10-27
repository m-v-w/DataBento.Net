using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn.StatefulReader;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Mbp1Struct
{
    private const int StructSize = 16+64;
    
    public RecordHeader Header;
    public long Price;
    public uint Size;
    public byte Action;
    public byte Side;
    public byte Flags;
    public byte Depth;
    public ulong TsRecv;
    public int TsInDelta;
    public uint Seq;
    public long BidPx;
    public long AskPx;
    public uint BidSize;
    public uint AskSize;
    public uint BidCount;
    public uint AskCount;
    
    
    public uint InstrumentId => Header.InstrumentId;
    public ulong TsEvent => Header.TsEvent;
    public decimal PriceDecimal => Price / 1_000_000_000M;
    public decimal BidDecimal => BidPx / 1_000_000_000M;
    public decimal AskDecimal => AskPx / 1_000_000_000M;
    public RecordAction RecordAction => (RecordAction) Action;
    public RecordSide RecordSide => (RecordSide) Side;
    public RecordFlags RecordFlags => (RecordFlags) Flags;
    public static ref Mbp1Struct UnsafeReference(ReadOnlySpan<byte> span)
    {
        if(span.Length != StructSize)
            throw new ArgumentException($"Mbp1 struct must be {StructSize} bytes");
        return ref Unsafe.As<byte, Mbp1Struct>(ref MemoryMarshal.GetReference(span));
    }
}