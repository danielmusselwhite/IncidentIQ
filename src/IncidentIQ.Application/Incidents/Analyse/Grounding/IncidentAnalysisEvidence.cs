namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Represents the exact grounding evidence supplied during an Incident analysis.
/// </summary>
public sealed record IncidentAnalysisEvidence(
    IReadOnlyList<HistoricalIncidentEvidence> HistoricalIncidents,
    IReadOnlyList<RunbookChunkEvidence> RunbookChunks);