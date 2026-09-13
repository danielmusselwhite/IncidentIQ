namespace IncidentIQ.Api.Contracts.Assistants;

public sealed record OperationalAnswerSectionResponse(
    string Content,
    IReadOnlyList<string> EvidenceReferences);