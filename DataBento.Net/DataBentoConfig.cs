using DataBento.Net.Tcp.ControlMessages;

namespace DataBento.Net;

public class DataBentoConfig
{
    public required string ApiKey { get; set; }
    public CompressionMode CompressionMode { get; set; } = CompressionMode.None;
    public TimeSpan StaleConnectionTimeout { get; set; } = TimeSpan.FromSeconds(35);
    public bool TsOut { get; set; } = false;
    public TimeSpan? FlushMsgInterval { get; set; } = null;
}