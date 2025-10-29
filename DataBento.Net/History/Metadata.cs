using System.Text.Json.Serialization;

namespace DataBento.Net.History;


public class HistoryMetadataField
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
