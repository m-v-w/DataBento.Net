using System.Buffers;
using CommunityToolkit.HighPerformance;
using DataBento.Net.Dbn;

namespace DataBento.Net.Tcp.ControlMessages;

internal class SubscriptionRequest : IControlMsgWithSerializer
{
    [ControlMsgField("schema")]
    public required SchemaId Schema { get; init; }
    [ControlMsgField("stype_in")]
    public required SymbolType SymbolTypeIn { get; init; }
    [ControlMsgField("symbols")] // CSV separated
    public required string[] Symbols { get; init; }
    [ControlMsgField("start")]
    public long? Start { get; init; }
    [ControlMsgField("snapshot")]
    public bool? Snapshot { get; init; }
    [ControlMsgField("id")]
    public uint? Id { get; init; }
    [ControlMsgField("is_last")]
    public bool? IsLast { get; init; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteAscii("schema");
        writer.Write(ControlMsgSerializer.Equal);
        writer.WriteAscii(EnumSerializer<SchemaId>.ToString(Schema));
        
        writer.Write(ControlMsgSerializer.Pipe);
        writer.WriteAscii("stype_in");
        writer.Write(ControlMsgSerializer.Equal);
        writer.WriteAscii(EnumSerializer<SymbolType>.ToString(SymbolTypeIn));
        
        writer.Write(ControlMsgSerializer.Pipe);
        writer.WriteAscii("symbols");
        writer.Write(ControlMsgSerializer.Equal);
        for(var i=0; i<Symbols.Length; i++)
        {
            if (i > 0)
                writer.Write(ControlMsgSerializer.Comma);
            writer.WriteAscii(Symbols[i]);
        }
        if (Start.HasValue)
        {
            writer.Write(ControlMsgSerializer.Pipe);
            writer.WriteAscii("start");
            writer.Write(ControlMsgSerializer.Equal);
            writer.Write(Start.Value);
        }
        if (Snapshot.HasValue)
        {
            writer.Write(ControlMsgSerializer.Pipe);
            writer.WriteAscii("snapshot");
            writer.Write(ControlMsgSerializer.Equal);
            writer.Write(Snapshot.Value ? ControlMsgSerializer.One : ControlMsgSerializer.Zero);
        }
        if (Id.HasValue)
        {
            writer.Write(ControlMsgSerializer.Pipe);
            writer.WriteAscii("id");
            writer.Write(ControlMsgSerializer.Equal);
            writer.Write(Id.Value);
        }
        if (IsLast.HasValue)
        {
            writer.Write(ControlMsgSerializer.Pipe);
            writer.WriteAscii("is_last");
            writer.Write(ControlMsgSerializer.Equal);
            writer.Write(IsLast.Value ? ControlMsgSerializer.One : ControlMsgSerializer.Zero);
        }
        writer.Write(ControlMsgSerializer.NewLine);
    }
}