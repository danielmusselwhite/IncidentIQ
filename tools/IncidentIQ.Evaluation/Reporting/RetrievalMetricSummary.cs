namespace IncidentIQ.Evaluation.Reporting;

/// <summary>
/// Represents aggregate retrieval performance at a specific K value.
/// </summary>
internal sealed record RetrievalMetricSummary(
    int K,
    double MeanPrecision,
    double MeanRecall);