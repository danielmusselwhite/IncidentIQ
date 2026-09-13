using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Represents the grounded context used to analyse an Incident,
/// keeping historical Incident and Runbook evidence separate.
/// </summary>
public sealed record IncidentAnalysisContext(
    IncidentAnalysisInput Incident,
    IReadOnlyList<HistoricalIncidentMatch> HistoricalIncidents,
    IReadOnlyList<RunbookChunkMatch> RunbookChunks);