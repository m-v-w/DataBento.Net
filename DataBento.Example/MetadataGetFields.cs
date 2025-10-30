using System.Text.Json;
using DataBento.Net.Dbn;
using DataBento.Net.History;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataBento.Example;

public class MetadataGetFields
{
    public static async Task MetadataGetFieldsExample()
    { 
        var configuration = new ConfigurationBuilder().AddUserSecrets<MetadataGetFields>().Build();
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
        var fields = await client.MetadataListFields(SchemaId.Mbp1, CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(fields, new JsonSerializerOptions() {WriteIndented = true}));
        
    }
}