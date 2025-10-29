using System.Net.Http.Json;
using DataBento.Net.Dbn;
using DataBento.Net.Dbn.SchemaRecords;
using DataBento.Net.Tcp.ControlMessages;
using Microsoft.Extensions.Logging;

namespace DataBento.Net.History;

public class BentoHistoryClient
{
    internal const string HttpClientName = nameof(BentoHistoryClient);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public BentoHistoryClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public async Task<HistorySymbology> SymbologyResolve(string dataset, string symbols, SymbolType symbolTypeIn,
        SymbolType symbolTypeOut, DateOnly startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var parameters = new List<KeyValuePair<string, string>>()
        {
            new ("dataset", dataset),
            new ("symbols", symbols),
            new ("stype_in", EnumSerializer<SymbolType>.ToString(symbolTypeIn)),
            new ("stype_out", EnumSerializer<SymbolType>.ToString(symbolTypeOut)),
            new ("start_date", startDate.ToString("yyyy-MM-dd")),
        };
        if (endDate.HasValue)
            parameters.Add(new ("end_date", endDate.Value.ToString("yyyy-MM-dd")));
        using var response = await httpClient.PostAsync("v0/symbology.resolve", 
            new FormUrlEncodedContent(parameters), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HistorySymbology>(cancellationToken)
            ?? throw new InvalidDataException("Failed to deserialize response");
    }

    public async Task<(Metadata metadata, List<object> records)> TimeSeriesGetRange(string dataset, SchemaId schema, DateOnly start, DateOnly? end, 
        string? symbols, SymbolType? symbolTypeIn, SymbolType? symbolTypeOut, CancellationToken cancellationToken)
    {
        //if(start.Kind != DateTimeKind.Utc)
            //throw new ArgumentException("start must be in UTC", nameof(start));
        //if(end.HasValue && end.Value.Kind != DateTimeKind.Utc)
            //throw new ArgumentException("end must be in UTC", nameof(end));
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var parameters = new List<KeyValuePair<string, string>>()
        {
            new ("dataset", dataset),
            new ("start", start.ToString("yyyy-MM-dd")), 
            new ("schema", EnumSerializer<SchemaId>.ToString(schema)),
            new ("encoding", "dbn"),
        };
        if (end.HasValue)
            parameters.Add(new ("end", end.Value.ToString("yyyy-MM-dd")));
        if (!string.IsNullOrEmpty(symbols))
            parameters.Add(new ("symbols", symbols));
        if(symbolTypeIn.HasValue)
            parameters.Add(new ("stype_in", EnumSerializer<SymbolType>.ToString(symbolTypeIn.Value)));
        if(symbolTypeOut.HasValue)
            parameters.Add(new ("stype_out", EnumSerializer<SymbolType>.ToString(symbolTypeOut.Value)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "v0/timeseries.get_range");
        request.Content = new FormUrlEncodedContent(parameters);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var reader = new HistoryDbnReader(stream, _loggerFactory.CreateLogger("DbnReader"));
        await reader.Read(cancellationToken);
        return (reader.Metadata ?? throw new DbnSerializationError("No metadata in response"), reader.Records);
    }
    public async Task<HistoryMetadataField[]> MetadataListFields(SchemaId schema, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.GetAsync($"v0/metadata.list_fields" +
                                                       $"?schema={EnumSerializer<SchemaId>.ToString(schema)}&encoding=dbn", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HistoryMetadataField[]>(cancellationToken) 
            ?? throw new InvalidDataException("Failed to deserialize response");
    }
}