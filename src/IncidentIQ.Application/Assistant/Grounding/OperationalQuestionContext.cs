using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Application.Assistant.Grounding;

/// <summary>
/// Contains the user question and operational evidence retrieved to ground
/// a single Assistant response.
/// </summary>
public sealed record OperationalQuestionContext(
    string Question,
    string? Service,
    string? Environment,
    IReadOnlyList<HistoricalIncidentMatch> HistoricalIncidents,
    IReadOnlyList<RunbookChunkMatch> RunbookChunks);