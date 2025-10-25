using DataBento.Net.Dbn;
using DataBento.Net.Tcp;

namespace DataBento.Example;

public class PrintSystemMsgHandler : ISystemMsgHandler
{
    public ControlMsgResult Handle(SystemMessage systemMsg)
    {
        if (systemMsg.Message == "Heartbeat")
        {
            Console.WriteLine("Heartbeat");
            return ControlMsgResult.None;
        }
        Console.WriteLine("Received system message: {0}", systemMsg);
        return ControlMsgResult.None;
    }

    public ControlMsgResult Handle(ErrorMessage errorMsg)
    {
        Console.WriteLine("Received ERROR message: {0}", errorMsg);
        return ControlMsgResult.Failed;
    }
}