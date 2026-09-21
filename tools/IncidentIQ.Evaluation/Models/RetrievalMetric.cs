namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents retrieval quality measured at a particular rank cutoff.
/// </summary>
public sealed record RetrievalMetric(
    int K,
    int RelevantRetrieved,
    int RelevantExpected,
    double Precision,
    double Recall);