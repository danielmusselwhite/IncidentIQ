using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Api.Contracts.Operations;

public sealed record FailedIncidentResponse(
    string Id,
    string Title,
    string Service,
    string Environment,
    IncidentSeverity Severity,
    string? FailureReason,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? FailedAt)
{
    public static FailedIncidentResponse FromDomain(
        Incident incident)
    {
        return new FailedIncidentResponse(
            incident.Id,
            incident.Title,
            incident.Service,
            incident.Environment,
            incident.Severity,
            incident.FailureReason,
            incident.AttemptCount,
            incident.CreatedAt,
            incident.ProcessingStartedAt,
            incident.LastAttemptAt,
            incident.FailedAt);
    }
}