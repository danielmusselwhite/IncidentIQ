namespace IncidentIQ.Application.Runbooks.RetrieveChunks;

/// <summary>
/// Represents a Runbook chunk returned from semantic vector retrieval.
/// </summary>
public sealed record RunbookChunkMatch(
    Guid RunbookId,
    int ChunkIndex,
    string Title,
    string Service,
    string Content,
    double Distance);