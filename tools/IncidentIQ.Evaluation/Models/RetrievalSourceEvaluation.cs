namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Contains the calculated retrieval metrics for one evidence source.
/// </summary>
public sealed record RetrievalSourceEvaluation<T>(
    IReadOnlyList<T> Results,
    IReadOnlyList<RetrievalMetric> Metrics,
    bool ExpectsNoEvidence,
    bool? NoEvidenceCorrect);