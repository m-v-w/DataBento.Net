using System.Buffers;

namespace DataBento.Net.Tcp.ControlMessages;

public interface IControlMsg
{
}

public interface IControlMsgWithSerializer : IControlMsg
{
    public void Serialize(IBufferWriter<byte> writer);
}

public class ControlMsgFieldAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
public class ControlMsgResponseAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}