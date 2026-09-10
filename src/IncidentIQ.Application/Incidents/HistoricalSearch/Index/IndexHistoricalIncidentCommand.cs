namespace IncidentIQ.Application.Incidents.HistoricalSearch.Index;

/// <summary>
/// Requests asynchronous indexing of a completed Incident for semantic retrieval.
/// </summary>
public sealed record IndexHistoricalIncidentCommand(
    Guid CommandId,
    string IncidentId,
    string CorrelationId,
    DateTimeOffset QueuedAtUtc);