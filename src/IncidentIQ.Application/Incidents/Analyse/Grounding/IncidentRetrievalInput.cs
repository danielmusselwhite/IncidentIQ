namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Represents the semantic query and metadata filters used to retrieve
/// supporting evidence for an Incident analysis.
/// </summary>
public sealed record IncidentRetrievalInput(
    string QueryText,
    string Service,
    string Environment);