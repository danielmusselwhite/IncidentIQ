namespace IncidentIQ.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Represents the projected result returned by a Cosmos Runbook vector query.
/// </summary>
internal sealed class CosmosRunbookChunkMatchResult
{
    public string RunbookId { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Distance { get; init; }
}