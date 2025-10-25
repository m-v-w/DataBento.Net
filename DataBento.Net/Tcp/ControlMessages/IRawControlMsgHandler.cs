namespace DataBento.Net.Tcp.Msgs;

internal interface IRawControlMsgHandler
{
    void Handle(ReadOnlySpan<char> msg);
}