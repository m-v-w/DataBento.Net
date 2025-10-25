using DataBento.Net.Dbn;

namespace DataBento.Net.Tcp.ControlMessages;

internal interface IRawControlMsgHandler
{
    ControlMsgResult Handle(ReadOnlySpan<char> msgStr);
}

