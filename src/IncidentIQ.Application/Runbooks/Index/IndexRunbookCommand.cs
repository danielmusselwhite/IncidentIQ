namespace IncidentIQ.Application.Runbooks.Index;

/// <summary>
/// Represents a request to asynchronously index a Runbook for vector retrieval.
/// </summary>
public sealed record IndexRunbookCommand(
    Guid CommandId,
    Guid RunbookId,
    string CorrelationId,
    DateTime QueuedAtUtc,
    DateTime SourceUpdatedAtUtc);