namespace DataBento.Net.Tcp.Msgs;

public interface IControlMsg
{
    
}
public class ControlMsgFieldAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
public class ControlMsgResponseAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}