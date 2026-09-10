using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;

/// <summary>
/// Represents a historical Incident returned from semantic similarity retrieval.
/// </summary>
public sealed record HistoricalIncidentMatch(
    Guid IncidentId,
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    IncidentSeverity Severity,
    DateTimeOffset CompletedAtUtc,
    double Distance);