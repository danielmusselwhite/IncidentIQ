namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents a Runbook chunk returned during retrieval evaluation.
/// </summary>
public sealed record RetrievedRunbookChunk(
    Guid RunbookId,
    int ChunkIndex,
    int Rank,
    double Distance,
    bool IsExpected);