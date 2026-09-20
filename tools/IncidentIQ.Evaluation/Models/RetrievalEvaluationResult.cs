namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents the retrieval evaluation result for one controlled scenario.
/// </summary>
public sealed record RetrievalEvaluationResult(
    string CaseId,
    string CaseName,
    RetrievalSourceEvaluation<RetrievedHistoricalIncident> HistoricalIncidents,
    RetrievalSourceEvaluation<RetrievedRunbookChunk> Runbooks);