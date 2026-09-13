namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record AskOperationalQuestionRequest(
    string Question,
    string? Service,
    string? Environment,
    IReadOnlyList<ConversationTurnRequest>? ConversationHistory);