namespace IncidentIQ.Application.Runbooks.Index;

public sealed record IndexRunbookCommand(
    Guid CommandId,
    string RunbookId,
    string CorrelationId, // Used to correlate the indexing request throughout the system
    DateTimeOffset QueuedAtUtc
);