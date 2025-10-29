using DataBento.Net.Dbn;
using DataBento.Net.Dbn.SchemaRecords;
using DataBento.Net.History;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataBento.Example;

public class TimeSeriesGetRange
{
    public static async Task TimeSeriesGetRangeExample()
    { 
        var configuration = new ConfigurationBuilder().AddUserSecrets<TimeSeriesGetRange>().Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        serviceCollection.AddBentoHistoryClient(_ =>
            configuration["ApiKey"] ?? throw new InvalidProgramException("Missing ApiKey configuration"));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<BentoHistoryClient>();
        var (metadata, records) = await client.TimeSeriesGetRange("EQUS.SUMMARY", SchemaId.InstrumentDef, 
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5).Date),
            null, "AAPL,TSLA,IBIT,", SymbolType.RawSymbol, null, CancellationToken.None);
        Console.WriteLine(metadata);
        foreach (var record in records)
        {
            var e = (InstrumentDefStructV1)record;
            Console.WriteLine($"{TimestampUtils.UnixNanoToDateTime(e.Header.TsEvent):O} {e.Header.InstrumentId} {e.Header.PublisherId} " +
                              $"{e.RawSymbol} {e.Currency} {e.Exchange} " +
                              $"{TimestampUtils.UnixNanoToDateTime(e.Activation):O} {TimestampUtils.UnixNanoToDateTime(e.Expiration):O}");
        }
    }
}