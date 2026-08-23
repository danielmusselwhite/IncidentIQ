using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Api.Contracts.Incidents;

/// <summary>
/// Represents a request DTO to create an incident.
/// (What HTTP accepts)
/// </summary>
/// <param name="Title">The title of the incident.</param>
/// <param name="Description">The description of the incident.</param>
/// <param name="Service">The service affected by the incident.</param>
/// <param name="Environment">The environment in which the incident occurred.</param>
/// <param name="Severity">The severity of the incident.</param>
/// <param name="Symptoms">The symptoms observed for the incident.</param>
public sealed record CreateIncidentRequest(
    string Title,
    string Description,
    string Service,
    string Environment,
    IncidentSeverity Severity,
    string? Symptoms);