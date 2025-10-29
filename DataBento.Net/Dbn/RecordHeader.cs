using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn;

[StructLayout(LayoutKind.Sequential, Size=16, Pack=1)]
public struct RecordHeader
{
    internal const int StructSize = 16;
    public byte Length; // The length of the record in 32-bit words.
    public byte RType; //The record type. Each schema corresponds with a single rtype value.
    public ushort PublisherId; //The publisher ID assigned by Databento, which denotes the dataset and venue.
    public uint InstrumentId; // The numeric instrument ID.
    public ulong TsEvent; // The event timestamp as the number of nanoseconds since the UNIX epoch.
    
    public static ref RecordHeader UnsafeReference(ReadOnlySpan<byte> span)
    {
        if(span.Length < StructSize)
            throw new ArgumentException($"{nameof(RecordHeader)} struct must be at least {StructSize} bytes");
        return ref Unsafe.As<byte, RecordHeader>(ref MemoryMarshal.GetReference(span));
    }
}