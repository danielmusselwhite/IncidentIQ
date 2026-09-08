using Newtonsoft.Json;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Represents the projected result returned by a Cosmos Runbook vector query.
/// </summary>
internal sealed class CosmosRunbookChunkMatchResult
{
    [JsonProperty("runbookId")]
    public string RunbookId { get; init; } = string.Empty;

    [JsonProperty("chunkIndex")]
    public int ChunkIndex { get; init; }

    [JsonProperty("title")]
    public string Title { get; init; } = string.Empty;

    [JsonProperty("service")]
    public string Service { get; init; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; init; } = string.Empty;

    [JsonProperty("distance")]
    public double Distance { get; init; }
}