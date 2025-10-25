namespace DataBento.Net.Tcp.Msgs;

internal class GreetingMsg : IControlMsg
{
    [ControlMsgField("lsg_version")]
    public required string Version { get; set; }
}
internal class CramMsg : IControlMsg
{
    [ControlMsgField("cram")]
    public required string Cram { get; set; }
}
internal class AuthenticationMsg : IControlMsg
{
    [ControlMsgField("auth_token")]
    public required string AuthToken { get; set; }
    [ControlMsgField("dataset")]
    public required string Dataset { get; set; }
    [ControlMsgField("encoding")]
    public required StreamEncoding Encoding { get; set; }
    [ControlMsgField("ts_out")]
    public required bool TsOut { get; set; }
}