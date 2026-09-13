namespace IncidentIQ.Application.Assistant.Generate;

/// <summary>
/// Represents one part of a grounded Assistant answer together with the
/// evidence supporting that part.
/// </summary>
public sealed record OperationalAnswerSection(
    string Content,
    IReadOnlyList<string> EvidenceReferences);