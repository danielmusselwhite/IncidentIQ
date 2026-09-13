namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record OperationalAssistantResponse(
    IReadOnlyList<OperationalAnswerSectionResponse> Sections,
    AssistantEvidenceResponse Evidence,
    string Model,
    DateTimeOffset AnsweredAtUtc);