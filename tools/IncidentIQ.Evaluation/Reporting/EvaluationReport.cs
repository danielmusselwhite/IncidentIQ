using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Reporting;

/// <summary>
/// Represents the machine-readable output of a retrieval evaluation run.
/// </summary>
internal sealed record EvaluationReport(
    DateTimeOffset GeneratedAtUtc,
    string EmbeddingDeployment,
    string EmbeddingModel,
    int EmbeddingDimensions,
    int HistoricalIncidentCount,
    int RunbookCount,
    int EvaluationCaseCount,
    IReadOnlyList<RetrievalMetricSummary> HistoricalIncidentMetrics,
    IReadOnlyList<RetrievalMetricSummary> RunbookMetrics,
    NoEvidenceSummary NoEvidence,
    IReadOnlyList<RetrievalEvaluationResult> Cases);

/// <summary>
/// Represents the aggregate result of no-evidence evaluation cases.
/// </summary>
internal sealed record NoEvidenceSummary(
    int Passed,
    int Total);