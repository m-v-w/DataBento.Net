using System.Runtime.InteropServices;
using DataBento.Net.Dbn;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Mbp1Struct
{
    public RecordHeader Header;
    public long Price;
    public uint Size;
    public byte Action;
    public byte Side;
    public byte Flags;
    public byte Depth;
    public ulong TsRecv;
    public int TsInDelta;
    public long BidPx;
    public long AskPx;
    public uint BidSize;
    public uint AskSize;
    public uint BidCount;
    public uint AskCount;
}