using System.Security.Cryptography;
using System.Text;
using DataBento.Net.Dbn;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Logging;

namespace DataBento.Net.Tcp;

internal class ControlMsgHandler
{
    private readonly ILogger _logger;
    private readonly DataBentoTcpConfig _config;
    public string? LsgVersion { get; private set; }

    public ControlMsgHandler(DataBentoTcpConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task OnCram(CramMsg msg, ControlMsgClient client)
    {
        var response = new AuthenticationMsg()
        {
            AuthToken = CalculateAuthToken(msg),
            Dataset = _config.Dataset,
            Encoding = StreamEncoding.Dbn,
            TsOut = _config.TsOut,
            Compression = _config.CompressionMode
        };
        await client.Send(response);
    }
    private string CalculateAuthToken(CramMsg cramMsg)
    {
        var msg = $"{cramMsg.Cram}|{_config.ApiKey}";
        var msgBytes = Encoding.ASCII.GetBytes(msg);
        var hash = SHA256.HashData(msgBytes);
        var keySuffix = _config.ApiKey.Substring(_config.ApiKey.Length - 5, 5);
        return $"{Convert.ToHexString(hash).ToLowerInvariant()}-{keySuffix}";
    }
    private async Task OnAuthSuccess(AuthenticationResponseMsg msg, ControlMsgClient client)
    {
        _logger.LogInformation("Authentication succeeded session-id: {SessionId} {Error}", msg.SessionId, msg.Error);
        // Make sure the stream reader is set to state Metadata and ready for decompression
        await Task.Delay(TimeSpan.FromMilliseconds(100)); 
        await client.Send(new SessionStartMsg());
    }
    public ControlMsgResult Handle(ReadOnlySpan<char> msgStr, ControlMsgClient client)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("< {Msg}", new string(msgStr));    
        }
        var msg = ControlMsgSerializer.Deserialize(msgStr);
        switch(msg)
        {
            case GreetingMsg greetingMsg:
                _logger.LogInformation("Received greeting: {Version}", greetingMsg.Version);
                LsgVersion = greetingMsg.Version;
                break;
            case CramMsg cramMsg:
                Task.Run(() => OnCram(cramMsg, client));
                break;
            case AuthenticationResponseMsg authResp:
                if (!authResp.Success)
                {
                    _logger.LogError("Authentication failed: {Reason}", authResp.Error);
                    return ControlMsgResult.Failed;
                }
                Task.Run(() => OnAuthSuccess(authResp, client));
                return ControlMsgResult.PhaseComplete;
            default:
                _logger.LogWarning("Unknown control message: {Msg}", new string(msgStr));
                break;
        }
        return ControlMsgResult.None;
    }

    
}