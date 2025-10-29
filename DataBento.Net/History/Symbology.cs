using System.Text.Json.Serialization;
using DataBento.Net.Dbn;
using DataBento.Net.Tcp.ControlMessages;

namespace DataBento.Net.History;


public class HistorySymbologyResult
{
    [JsonPropertyName("d0")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("d1")]
    public DateOnly EndDate { get; set; }

    [JsonPropertyName("s")]
    public required string SymbolOut { get; set; }
}

public class HistorySymbology
{
    [JsonPropertyName("result")]
    public required Dictionary<string,HistorySymbologyResult[]> Result { get; set; }

    [JsonPropertyName("symbols")]
    public required List<string> Symbols { get; set; }

    [JsonPropertyName("stype_in")]
    public SymbolType SymbolTypeIn { get; set; }

    [JsonPropertyName("stype_out")]
    public SymbolType SymbolTypeOut { get; set; }

    [JsonPropertyName("start_date")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateOnly? EndDate { get; set; }

    [JsonPropertyName("partial")]
    public required List<string> Partial { get; set; }

    [JsonPropertyName("not_found")]
    public required List<string> NotFound { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

