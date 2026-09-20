using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Calculates deterministic retrieval metrics against known relevant evidence.
/// </summary>
public static class RetrievalMetricCalculator
{
    /// <summary>
    /// Calculates Precision@K and Recall@K for ranked retrieved evidence.
    /// </summary>
    /// <remarks>
    /// Duplicate retrieved source IDs count independently for precision because
    /// they occupy independent retrieval slots.
    ///
    /// Recall counts unique expected source IDs, preventing duplicate chunks
    /// from increasing evidence coverage.
    /// </remarks>
    public static IReadOnlyList<RetrievalMetric> Calculate(
        IReadOnlyList<Guid> retrievedIds,
        IReadOnlyList<Guid> expectedIds,
        IReadOnlyList<int> kValues)
    {
        ArgumentNullException.ThrowIfNull(retrievedIds);
        ArgumentNullException.ThrowIfNull(expectedIds);
        ArgumentNullException.ThrowIfNull(kValues);

        var expected = expectedIds.ToHashSet();

        if (expected.Count == 0)
        {
            return [];
        }

        var metrics =
            new List<RetrievalMetric>();

        foreach (var k in kValues)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

            var topK =
                retrievedIds
                    .Take(k)
                    .ToList();

            var relevantRetrieved =
                topK.Count(expected.Contains);

            var uniqueRelevantRetrieved =
                topK
                    .Where(expected.Contains)
                    .Distinct()
                    .Count();

            var precision = (double)relevantRetrieved / k;

            var recall = (double)uniqueRelevantRetrieved / expected.Count;

            metrics.Add(
                new RetrievalMetric(
                    K: k,
                    RelevantRetrieved: relevantRetrieved,
                    RelevantExpected: expected.Count,
                    Precision: precision,
                    Recall: recall));
        }

        return metrics;
    }
}