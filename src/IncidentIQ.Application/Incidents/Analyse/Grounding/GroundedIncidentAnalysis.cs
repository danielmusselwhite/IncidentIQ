namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Represents a persisted Incident analysis together with the exact
/// grounding evidence used to generate it.
/// </summary>
public sealed record GroundedIncidentAnalysis(
    IncidentAnalysisResult Analysis,
    IncidentAnalysisEvidence Evidence);