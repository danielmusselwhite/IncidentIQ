namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record OperationalAnswerSectionResponse(
    string Content,
    IReadOnlyList<string> EvidenceReferences);