using DataBento.Net.Dbn;
using DataBento.Net.Dbn.SchemaRecords;
using DataBento.Net.History;
using DataBento.Net.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataBento.Example;

public class TimeSeriesGetRange
{
    private const string Dataset = "OPRA.PILLAR";
    private static readonly SchemaId SchemaId = SchemaId.Cmbp1;
    private const string Symbols = "TSLA  251031C00467500";
    public static void Run()
    {
        TimeSeriesGetRangeExample().GetAwaiter().GetResult();
    }
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
        var (metadata, records) = await client.TimeSeriesGetRange(Dataset, SchemaId, 
            DateOnly.FromDateTime(new DateTime(2025,10,30, 14,0,0, DateTimeKind.Utc)),
            null, Symbols, SymbolType.RawSymbol, SymbolType.InstrumentId,100);
        Console.WriteLine(metadata);
        foreach (var record in records)
        {
            if (record is InstrumentDefStructV1 e)
            {
                Console.WriteLine($"{TimestampUtils.UnixNanoToDateTime(e.Header.TsEvent):O} {e.Header.InstrumentId} {e.Header.PublisherId} " +
                                  $"{e.RawSymbol} {e.Currency} {e.Exchange} " +
                                  $"{TimestampUtils.UnixNanoToDateTime(e.Activation):O} {TimestampUtils.UnixNanoToDateTime(e.Expiration):O}");
                continue;
            }
            if (record is Mbp1Struct q)
            {
                Console.WriteLine(
                    $"{TimestampUtils.UnixNanoToDateTime(q.Header.TsEvent):O} {q.Header.InstrumentId} {q.Header.PublisherId} " +
                    $"Bid: {q.BidDecimal}@{q.BidSz00} / Ask: {q.AskDecimal}@{q.AskSz00}");
                continue;
            }
        }
    }
}