using IncidentIQ.Application.Runbooks.Index;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a vectorised Runbook chunk.
/// </summary>
internal sealed class RunbookChunkDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Partition key shared by every chunk derived from the same Runbook.
    /// </summary>
    [JsonPropertyName("runbookId")]
    public required string RunbookId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Content { get; init; }

    public required string Title { get; init; }

    public required string Service { get; init; }

    public DateTime SourceUpdatedAtUtc { get; init; }

    // Explicitly name this property because the Cosmos vector policy targets
    // the exact /embedding JSON path.
    [JsonPropertyName("embedding")]
    public required IReadOnlyList<float> Embedding { get; init; }

    /// <summary>
    /// Generates a stable ID so re-indexing the same Runbook does not create
    /// duplicate documents for the same chunk position.
    /// </summary>
    private static string GenerateId(Guid runbookId, int chunkIndex) =>
        $"{runbookId}-chunk-{chunkIndex}";

    internal static RunbookChunkDocument FromApplication(RunbookChunk runbookChunk)
    {
        return new RunbookChunkDocument
        {
            Id = GenerateId(runbookChunk.RunbookId, runbookChunk.ChunkIndex),
            RunbookId = runbookChunk.RunbookId.ToString(),
            ChunkIndex = runbookChunk.ChunkIndex,
            Content = runbookChunk.Content,
            Title = runbookChunk.Title,
            Service = runbookChunk.Service,
            SourceUpdatedAtUtc = runbookChunk.SourceUpdatedAtUtc,
            Embedding = runbookChunk.Embedding
        };
    }
}