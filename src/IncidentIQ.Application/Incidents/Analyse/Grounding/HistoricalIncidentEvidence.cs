using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Represents a snapshot of a historical Incident supplied as grounding evidence.
/// </summary>
public sealed record HistoricalIncidentEvidence(
    string ReferenceId,
    Guid IncidentId,
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    IncidentSeverity Severity,
    DateTimeOffset CompletedAtUtc,
    double Distance);