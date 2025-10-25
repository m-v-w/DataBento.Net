using DataBento.Net.Tcp.ControlMessages;

namespace DataBento.Net.Tcp;

public class DataBentoTcpConfig
{
    public required string ApiKey { get; init; }
    public required string Dataset { get; init; }
    public CompressionMode CompressionMode { get; init; } = CompressionMode.None;
    public bool TsOut { get; init; } = false;
}