using DataBento.Net.Tcp.ControlMessages;

namespace DataBento.Net;

public class DataBentoConfig
{
    public required string ApiKey { get; init; }
    public CompressionMode CompressionMode { get; init; } = CompressionMode.None;
    public bool TsOut { get; init; } = false;
}