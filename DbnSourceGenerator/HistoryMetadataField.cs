using System.Text.Json.Serialization;

namespace DbnSourceGenerator;

public class HistoryMetadataField
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}