using IncidentIQ.Application.Incidents.HistoricalSearch;
using IncidentIQ.Application.Runbooks.Index;

namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Contains the vectorised synthetic corpus used during retrieval evaluation.
/// </summary>
public sealed record EvaluationCorpusIndex(
    IReadOnlyList<HistoricalIncidentVector> HistoricalIncidents,
    IReadOnlyList<RunbookChunk> RunbookChunks);