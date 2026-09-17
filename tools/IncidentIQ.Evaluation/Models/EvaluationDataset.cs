namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents the complete version-controlled dataset used by the
/// IncidentIQ evaluation tooling.
/// </summary>
public sealed record EvaluationDataset(
    IReadOnlyList<EvaluationHistoricalIncident> HistoricalIncidents,
    IReadOnlyList<EvaluationRunbook> Runbooks,
    IReadOnlyList<EvaluationCase> Cases);