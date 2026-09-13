namespace IncidentIQ.Api.Contracts.Assistants;

public sealed record OperationalAssistantResponse(
    IReadOnlyList<OperationalAnswerSectionResponse> Sections,
    AssistantEvidenceResponse Evidence,
    string Model,
    DateTimeOffset AnsweredAtUtc);