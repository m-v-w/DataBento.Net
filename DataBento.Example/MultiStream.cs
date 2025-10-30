using DataBento.Net;
using DataBento.Net.Dbn;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataBento.Example;

public class MultiStream
{
    public static void Run()
    {
        MultiStreamExample().GetAwaiter().GetResult();
    }
    public static async Task MultiStreamExample()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<LiveSubscription>().Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        serviceCollection.AddDataBentoClient(o =>
        {
            o.ApiKey = configuration["ApiKey"] ?? throw new InvalidProgramException("Missing ApiKey configuration");
            o.CompressionMode = CompressionMode.None;
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<DataBentoClient>();
        var subs = (SubscriptionContainer) serviceProvider.GetRequiredService<ISubscriptionContainer>();
        await client.StartAsync(CancellationToken.None);
        await client.Subscribe("EQUS.MINI", [
            new SubscriptionKey("EQUS.MINI", SchemaId.Mbp1, SymbolType.RawSymbol, "AAPL,TSLA,MSFT")],
            new PrintSubscriptionMsgHandler(subs.GetOrAdd("EQUS.MINI")), CancellationToken.None);
        Console.WriteLine("Press ENTER key to exit...");
        Console.ReadLine();
        await client.StopAsync(CancellationToken.None);
        
    }
}