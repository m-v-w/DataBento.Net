using System.Buffers;
using CommunityToolkit.HighPerformance;

namespace DataBento.Net.Tcp.ControlMessages;

internal class SubscriptionRequest : IControlMsgWithSerializer
{
    [ControlMsgField("schema")]
    public required string Schema { get; set; }
    [ControlMsgField("stype_in")]
    public required string SymbolTypeIn { get; set; }
    [ControlMsgField("symbols")] // CSV separated
    public required string Symbols { get; set; }
    [ControlMsgField("start")]
    public long? Start { get; set; }
    [ControlMsgField("snapshot")]
    public bool? Snapshot { get; set; }
    [ControlMsgField("id")]
    public uint? Id { get; set; }
    [ControlMsgField("is_last")]
    public bool? IsLast { get; set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteAscii("schema");
        writer.Write(ControlMsgSerializer.Equal);
        writer.WriteAscii(Schema);
        
        writer.Write(ControlMsgSerializer.Pipe);
        writer.WriteAscii("stype_in");
        writer.Write(ControlMsgSerializer.Equal);
        writer.WriteAscii(SymbolTypeIn);
        
        writer.Write(ControlMsgSerializer.Pipe);
        writer.WriteAscii("symbols");
        writer.Write(ControlMsgSerializer.Equal);
        writer.WriteAscii(Symbols);
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