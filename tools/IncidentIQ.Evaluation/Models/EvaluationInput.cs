namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents the input to an evaluation scenario.
///
/// Operational Assistant scenarios use Question and optional retrieval filters.
/// Incident-analysis scenarios use Incident.
/// </summary>
public sealed record EvaluationInput(
    string? Question,
    EvaluationIncidentInput? Incident,
    string? ServiceFilter = null,
    string? EnvironmentFilter = null);