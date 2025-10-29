using DataBento.Net.Dbn;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataBento.Example;

public class Performance
{
    public static async Task PerformanceExample()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddUserSecrets<Performance>().Build();
        var dataset = "EQUS.MINI";
        var tcpConfig = new DataBentoTcpConfig()
        {
            ApiKey = configuration["ApiKey"] ?? throw new InvalidProgramException("Missing ApiKey configuration"), 
            CompressionMode = CompressionMode.None, 
            Dataset = dataset, 
            TsOut = true, 
            FlushMsgInterval = TimeSpan.FromSeconds(1)
        };
        var subs = new SubscriptionContainer();
        var handler = new CountSubscriptionMsgHandler();
        subs.Add(new SubscriptionKey(dataset, SchemaId.Mbp1, SymbolType.RawSymbol, "ALL_SYMBOLS"));
        using var client = new BentoTcpClient(loggerFactory, new ExponentialBackoffRetryPolicy(TimeSpan.FromSeconds(10)), 
            handler, tcpConfig, subs);
        await client.StartAsync(CancellationToken.None);
        Console.WriteLine("Press ENTER key to exit...");
        Console.ReadLine();
        Console.WriteLine();
        var latencyQuantiles = handler.Quantiles();
        Console.WriteLine($"Latency-Quantiles (ms): {string.Join(", ", latencyQuantiles)}");
        await client.StopAsync(CancellationToken.None);
    }
}