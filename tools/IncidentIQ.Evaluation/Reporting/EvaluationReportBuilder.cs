using IncidentIQ.Evaluation.Citations;
using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Reporting;

/// <summary>
/// Builds aggregate retrieval evaluation reports from individual case results.
/// </summary>
internal static class EvaluationReportBuilder
{
    public static EvaluationReport Build(
    IReadOnlyList<RetrievalEvaluationResult> results,
    IReadOnlyList<CitationEvaluationResult> citationResults,
    int historicalIncidentCount,
    int runbookCount,
    string embeddingDeployment,
    string embeddingModel,
    int embeddingDimensions)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(citationResults);

        if (string.IsNullOrWhiteSpace(embeddingDeployment))
        {
            throw new ArgumentException(
                "Embedding deployment is required.",
                nameof(embeddingDeployment));
        }

        if (string.IsNullOrWhiteSpace(embeddingModel))
        {
            throw new ArgumentException(
                "Embedding model is required.",
                nameof(embeddingModel));
        }

        if (embeddingDimensions <= 0)
        {
            throw new ArgumentException(
                "Embedding dimensions must be a positive integer.",
                nameof(embeddingDimensions));
        }

        var historicalIncidentMetrics =
            BuildMetricSummary(
                results.SelectMany(
                    result => result.HistoricalIncidents.Metrics));

        var runbookMetrics =
            BuildMetricSummary(
                results.SelectMany(
                    result => result.Runbooks.Metrics));

        var noEvidenceSummary =
            BuildNoEvidenceSummary(results);

        var citationEvaluation =
            CitationEvaluationSummaryBuilder.Build(
                citationResults);

        return new EvaluationReport(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            EmbeddingDeployment: embeddingDeployment,
            EmbeddingModel: embeddingModel,
            EmbeddingDimensions: embeddingDimensions,
            HistoricalIncidentCount: historicalIncidentCount,
            RunbookCount: runbookCount,
            EvaluationCaseCount: results.Count,
            HistoricalIncidentMetrics: historicalIncidentMetrics,
            RunbookMetrics: runbookMetrics,
            NoEvidence: noEvidenceSummary,
            CitationEvaluation: citationEvaluation,
            Cases: results,
            CitationCases: citationResults);
    }

    private static IReadOnlyList<RetrievalMetricSummary> BuildMetricSummary(
        IEnumerable<RetrievalMetric> metrics)
    {
        return metrics
            .GroupBy(metric => metric.K)
            .OrderBy(group => group.Key)
            .Select(group =>
                new RetrievalMetricSummary(
                    K: group.Key,
                    MeanPrecision:
                        group.Average(metric => metric.Precision),
                    MeanRecall:
                        group.Average(metric => metric.Recall)))
            .ToList();
    }

    private static NoEvidenceSummary BuildNoEvidenceSummary(
        IReadOnlyList<RetrievalEvaluationResult> results)
    {
        var checks =
            results
                .SelectMany(result => new bool?[]
                {
                    result.HistoricalIncidents.ExpectsNoEvidence
                        ? result.HistoricalIncidents.NoEvidenceCorrect
                        : null,

                    result.Runbooks.ExpectsNoEvidence
                        ? result.Runbooks.NoEvidenceCorrect
                        : null
                })
                .Where(result => result.HasValue)
                .Select(result => result!.Value)
                .ToList();

        return new NoEvidenceSummary(
            Passed: checks.Count(result => result),
            Total: checks.Count);
    }
}