namespace DataBento.Net.Tcp.ControlMessages;

[ControlMsgResponse("lsg_version")]
internal class GreetingMsg : IControlMsg
{
    [ControlMsgField("lsg_version")]
    public required string Version { get; set; }
}
[ControlMsgResponse("cram")]
internal class CramMsg : IControlMsg
{
    [ControlMsgField("cram")]
    public required string Cram { get; set; }
}
internal class AuthenticationMsg : IControlMsg
{
    [ControlMsgField("auth")]
    public required string AuthToken { get; set; }
    [ControlMsgField("dataset")]
    public required string Dataset { get; set; }
    [ControlMsgField("encoding")]
    public StreamEncoding? Encoding { get; set; }
    [ControlMsgField("compression")]
    public CompressionMode? Compression { get; set; }
    [ControlMsgField("ts_out")]
    public bool? TsOut { get; set; }
    [ControlMsgField("pretty_px")]
    public bool? PrettyPrice { get; set; }
    [ControlMsgField("pretty_ts")]
    public bool? PrettyTimestamp { get; set; }
    [ControlMsgField("heartbeat_interval_s")]
    public uint? HeartbeatIntervalSeconds { get; set; }
}
[ControlMsgResponse("success")]
internal class AuthenticationResponseMsg : IControlMsg
{
    [ControlMsgField("success")]
    public bool Success { get; set; }
    [ControlMsgField("error")] // optional
    public string? Error { get; set; }
    [ControlMsgField("session_id")] // optional
    public string? SessionId { get; set; }
}

internal class SessionStartMsg : IControlMsg
{
    [ControlMsgField("start_session")]
    public int StartSession { get; set; }
}