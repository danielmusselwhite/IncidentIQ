namespace IncidentIQ.Api.Contracts.Assistants;

public sealed record AskOperationalQuestionRequest(
    string Question,
    string? Service,
    string? Environment);