using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents a synthetic completed Incident used as searchable historical
/// evidence during AI evaluation.
/// </summary>
public sealed record EvaluationHistoricalIncident(
    Guid IncidentId,
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    IncidentSeverity Severity,
    DateTimeOffset CompletedAtUtc);