using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Api.Contracts.Incidents;

/// <summary>
/// Represents a response DTO for an incident.
/// </summary>
/// <param name="Id">The unique identifier of the incident.</param>
/// <param name="Title">The title of the incident.</param>
/// <param name="Description">The description of the incident.</param>
/// <param name="Service">The service affected by the incident.</param>
/// <param name="Environment">The environment in which the incident occurred.</param>
/// <param name="Severity">The severity of the incident.</param>
/// <param name="Symptoms">The symptoms observed for the incident.</param>
/// <param name="Status">The current status of the incident.</param>
/// <param name="CreatedAt">The timestamp when the incident was created.</param>
/// <param name="UpdatedAt">The timestamp when the incident was last updated.</param>
public sealed record IncidentResponse(
    string Id,
    string Title,
    string Description,
    string Service,
    string Environment,
    IncidentSeverity Severity,
    string? Symptoms,
    IncidentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Creates an instance of <see cref="IncidentResponse"/> from a domain (entity) incident.
    /// </summary>
    /// <param name="incident">The domain incident.</param>
    /// <returns>An instance of <see cref="IncidentResponse"/>.</returns>
    public static IncidentResponse FromDomain(Incident incident)
    {
        return new IncidentResponse(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Service,
            incident.Environment,
            incident.Severity,
            incident.Symptoms,
            incident.Status,
            incident.CreatedAt,
            incident.UpdatedAt);
    }
}