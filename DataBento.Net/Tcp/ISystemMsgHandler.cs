using DataBento.Net.Dbn;

namespace DataBento.Net.Tcp;

public interface ISystemMsgHandler
{
    ControlMsgResult Handle(SystemMessage systemMsg);
    ControlMsgResult Handle(ErrorMessage errorMsg);
}

public enum ControlMsgResult
{
    None, Failed, PhaseComplete
}