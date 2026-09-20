using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Writes human-readable retrieval evaluation results to the console.
/// </summary>
public static class RetrievalConsoleReporter
{
    public static void WriteCase(
        RetrievalEvaluationResult result)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"=== {result.CaseId} — {result.CaseName} ===");

        WriteHistoricalIncidents(
            result.HistoricalIncidents);

        WriteRunbooks(
            result.Runbooks);
    }

    public static void WriteSummary(
        IReadOnlyList<RetrievalEvaluationResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("Retrieval Evaluation Summary");
        Console.WriteLine("========================================");

        Console.WriteLine();
        Console.WriteLine("Historical Incidents");

        WriteMetricSummary(
            results
                .SelectMany(result =>
                    result.HistoricalIncidents.Metrics)
                .ToList());

        Console.WriteLine();
        Console.WriteLine("Runbooks");

        WriteMetricSummary(
            results
                .SelectMany(result =>
                    result.Runbooks.Metrics)
                .ToList());

        WriteNoEvidenceSummary(results);
    }

    private static void WriteHistoricalIncidents(
        RetrievalSourceEvaluation<RetrievedHistoricalIncident> evaluation)
    {
        Console.WriteLine();
        Console.WriteLine("Historical Incidents");

        foreach (var result in evaluation.Results)
        {
            Console.WriteLine(
                $"  #{result.Rank} " +
                $"{result.IncidentId} " +
                $"distance={result.Distance:F4} " +
                $"expected={result.IsExpected}");
        }

        WriteMetrics(evaluation);
    }

    private static void WriteRunbooks(
        RetrievalSourceEvaluation<RetrievedRunbookChunk> evaluation)
    {
        Console.WriteLine();
        Console.WriteLine("Runbook Chunks");

        foreach (var result in evaluation.Results)
        {
            Console.WriteLine(
                $"  #{result.Rank} " +
                $"{result.RunbookId} " +
                $"chunk={result.ChunkIndex} " +
                $"distance={result.Distance:F4} " +
                $"expected={result.IsExpected}");
        }

        WriteMetrics(evaluation);
    }

    private static void WriteMetrics<T>(
        RetrievalSourceEvaluation<T> evaluation)
    {
        if (evaluation.ExpectsNoEvidence)
        {
            Console.WriteLine(
                $"  No-evidence result: " +
                $"{(evaluation.NoEvidenceCorrect == true ? "PASS" : "FAIL")}");

            return;
        }

        foreach (var metric in evaluation.Metrics)
        {
            Console.WriteLine(
                $"  P@{metric.K}={metric.Precision:F3}  " +
                $"R@{metric.K}={metric.Recall:F3}");
        }
    }

    private static void WriteMetricSummary(
        IReadOnlyList<RetrievalMetric> metrics)
    {
        foreach (var group in metrics
                     .GroupBy(metric => metric.K)
                     .OrderBy(group => group.Key))
        {
            var averagePrecision =
                group.Average(metric => metric.Precision);

            var averageRecall =
                group.Average(metric => metric.Recall);

            Console.WriteLine(
                $"  Mean P@{group.Key}: {averagePrecision:F3}");

            Console.WriteLine(
                $"  Mean R@{group.Key}: {averageRecall:F3}");
        }
    }

    private static void WriteNoEvidenceSummary(
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

        if (checks.Count == 0)
        {
            return;
        }

        var passed =
            checks.Count(result => result);

        Console.WriteLine();
        Console.WriteLine(
            $"No-evidence checks: {passed}/{checks.Count} passed");
    }
}