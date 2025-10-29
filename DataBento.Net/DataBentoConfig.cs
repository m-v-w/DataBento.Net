using DataBento.Net.Tcp.ControlMessages;

namespace DataBento.Net;

public class DataBentoConfig
{
    public required string ApiKey { get; init; }
    public CompressionMode CompressionMode { get; init; } = CompressionMode.None;
    public TimeSpan StaleConnectionTimeout { get; init; } = TimeSpan.FromSeconds(35);
    public bool TsOut { get; init; } = false;
    public TimeSpan? FlushMsgInterval { get; init; } = null;
}