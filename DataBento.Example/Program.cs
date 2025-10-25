using DataBento.Example;
using DataBento.Net.Tcp;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var tcpConfig = new DataBentoTcpConfig()
{
    ApiKey = configuration["ApiKey"] ?? throw new InvalidProgramException("Missing ApiKey configuration"), 
    CompressionMode = CompressionMode.None, 
    Dataset = "EQUS.MINI",
    TsOut = false
};
using var client = new BentoTcpClient(loggerFactory, new ExponentialBackoffRetryPolicy(TimeSpan.FromSeconds(10)), 
    new PrintSubscriptionHandler(), tcpConfig, new PrintSystemMsgHandler());
await client.StartAsync(CancellationToken.None);
await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
Console.WriteLine("Subscribing...");
await client.Subscribe(["TSLA"]);
Console.WriteLine("Press ENTER key to exit...");
Console.ReadLine();
await client.StopAsync(CancellationToken.None);
