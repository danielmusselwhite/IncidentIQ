using IncidentIQ.Evaluation.Models;
using IncidentIQ.Evaluation.Reporting;

namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Writes human-readable retrieval evaluation results to the console.
/// </summary>
internal static class RetrievalConsoleReporter
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
        EvaluationReport report)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("Retrieval Evaluation Summary");
        Console.WriteLine("========================================");

        Console.WriteLine();
        Console.WriteLine("Historical Incidents");

        WriteMetricSummary(
            report.HistoricalIncidentMetrics);

        Console.WriteLine();
        Console.WriteLine("Runbooks");

        WriteMetricSummary(
            report.RunbookMetrics);

        if (report.NoEvidence.Total > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"No-evidence checks: " +
                $"{report.NoEvidence.Passed}/{report.NoEvidence.Total} passed");
        }
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
        IReadOnlyList<RetrievalMetricSummary> metrics)
    {
        foreach (var metric in metrics)
        {
            Console.WriteLine(
                $"  Mean P@{metric.K}: {metric.MeanPrecision:F3}");

            Console.WriteLine(
                $"  Mean R@{metric.K}: {metric.MeanRecall:F3}");
        }
    }
}