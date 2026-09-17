using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents the Incident input used by an Incident-analysis evaluation scenario.
/// </summary>
public sealed record EvaluationIncidentInput(
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    IncidentSeverity Severity);