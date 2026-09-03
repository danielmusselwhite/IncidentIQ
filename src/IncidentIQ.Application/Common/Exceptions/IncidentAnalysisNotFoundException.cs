namespace IncidentIQ.Application.Common.Exceptions;

/// <summary>
/// Thrown when no persisted analysis can be found for an incident.
/// </summary>
public sealed class IncidentAnalysisNotFoundException(string incidentId)
    : Exception($"Analysis for incident '{incidentId}' was not found.");
