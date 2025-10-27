using DataBento.Example;
using DataBento.Net.Dbn;
using DataBento.Net.Subscriptions;
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
var dataset = "EQUS.MINI";
var tcpConfig = new DataBentoTcpConfig()
{
    ApiKey = configuration["ApiKey"] ?? throw new InvalidProgramException("Missing ApiKey configuration"), 
    CompressionMode = CompressionMode.None, 
    Dataset = dataset,
    TsOut = false
};
var subs = new SubscriptionContainer();
subs.Add(new SubscriptionKey(dataset, SchemaId.Mbp1, SymbolType.RawSymbol, "TSLA"));
using var client = new BentoTcpClient(loggerFactory, new ExponentialBackoffRetryPolicy(TimeSpan.FromSeconds(10)), 
    new PrintSubscriptionMsgHandler(subs.GetOrAdd(dataset)), tcpConfig, subs);
await client.StartAsync(CancellationToken.None);
await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
Console.WriteLine("Subscribing...");
await client.Subscribe([new SubscriptionKey(dataset, SchemaId.Mbp1, SymbolType.RawSymbol, "AAPL")]);
Console.WriteLine("Press ENTER key to exit...");
Console.ReadLine();
await client.StopAsync(CancellationToken.None);
