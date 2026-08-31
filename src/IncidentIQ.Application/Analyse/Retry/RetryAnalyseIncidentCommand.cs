namespace IncidentIQ.Application.Analyse.Retry;

/// <summary>
/// Represents a request to retry analysis for a failed Incident.
/// </summary>
public sealed record RetryAnalyseIncidentCommand(
    string IncidentId,
    string CorrelationId // Used to correlate the analysis request throughout the system
);