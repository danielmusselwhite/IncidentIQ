namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Identifies the source documents considered relevant to an evaluation scenario.
/// </summary>
public sealed record ExpectedEvidence(
    IReadOnlyList<Guid> HistoricalIncidentIds,
    IReadOnlyList<Guid> RunbookIds);