using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Application.Assistant.Grounding;

/// <summary>
/// Represents a grounded Assistant response together with the evidence
/// retrieved for that response.
/// </summary>
public sealed record OperationalAssistantResult(
    OperationalAnswer Answer,
    IReadOnlyList<HistoricalIncidentMatch> HistoricalIncidents,
    IReadOnlyList<RunbookChunkMatch> RunbookChunks);