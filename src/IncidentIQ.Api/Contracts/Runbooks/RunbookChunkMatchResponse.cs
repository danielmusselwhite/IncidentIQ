using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Api.Contracts.Runbooks;

/// <summary>
/// Represents a Runbook chunk returned from semantic search.
/// </summary>
public sealed class RunbookChunkMatchResponse
{
    public Guid RunbookId { get; init; }
    public int ChunkIndex { get; init; }
    public required string Title { get; init; }
    public required string Service { get; init; }
    public required string Content { get; init; }
    public double Distance { get; init; }

    internal static RunbookChunkMatchResponse FromApplication(RunbookChunkMatch match)
    {
        return new RunbookChunkMatchResponse
        {
            RunbookId = match.RunbookId,
            ChunkIndex = match.ChunkIndex,
            Title = match.Title,
            Service = match.Service,
            Content = match.Content,
            Distance = match.Distance
        };
    }
}