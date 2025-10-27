using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn;

[StructLayout(LayoutKind.Sequential, Size=16, Pack=1)]
public struct RecordHeader
{
    public byte Length; // The length of the record in 32-bit words.
    public byte RType; //The record type. Each schema corresponds with a single rtype value.
    public ushort PublisherId; //The publisher ID assigned by Databento, which denotes the dataset and venue.
    public uint InstrumentId; // The numeric instrument ID.
    public ulong TsEvent; // The event timestamp as the number of nanoseconds since the UNIX epoch.
}