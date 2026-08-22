namespace IncidentIQ.Domain.Incidents;

/// <summary>
/// Represents the status of an incident.
/// </summary>
public enum IncidentStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}