using IncidentIQ.Application.Runbooks.Index;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos DB persistence representation of a vectorised Runbook chunk.
/// Multiple chunks belonging to the same Runbook share the same logical partition.
/// </summary>
public sealed class RunbookChunkDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Used as the Cosmos partition key so all chunks derived from one Runbook
    /// are stored in the same logical partition.
    /// </summary>
    [JsonPropertyName("runbookId")]
    public required string RunbookId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Content { get; init; }

    public required string Title { get; init; }

    public required string Service { get; init; }

    public DateTime SourceUpdatedAtUtc { get; init; }

    /// <summary>
    /// Numeric vector representation of this chunk used by Cosmos vector search.
    /// </summary>
    public required IReadOnlyList<float> Embedding { get; init; }

    /// <summary>
    /// Creates a deterministic document ID so processing the same Runbook
    /// revision more than once does not create duplicate chunk documents.
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

    internal RunbookChunk ToApplication() => new(
        Guid.Parse(RunbookId),
        ChunkIndex,
        Content,
        Title,
        Service,
        SourceUpdatedAtUtc,
        Embedding);
}