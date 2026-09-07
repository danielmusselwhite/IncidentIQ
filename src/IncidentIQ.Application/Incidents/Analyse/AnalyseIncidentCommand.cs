namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Represents a request for the Worker to asynchronously analyse an Incident.
/// Does not require the entire incident to be loaded into memory, only the IncidentId is required.
/// </summary>
public sealed record AnalyseIncidentCommand(
    Guid CommandId,
    string IncidentId,
    string CorrelationId, // Used to correlate the analysis request throughout the system
    DateTimeOffset QueuedAtUtc);