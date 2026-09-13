namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Represents a snapshot of a Runbook chunk supplied as grounding evidence.
/// </summary>
public sealed record RunbookChunkEvidence(
    string ReferenceId,
    Guid RunbookId,
    int ChunkIndex,
    string Title,
    string Service,
    string Content,
    double Distance);