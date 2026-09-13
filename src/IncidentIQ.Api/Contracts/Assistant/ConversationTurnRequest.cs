namespace IncidentIQ.Api.Contracts.Assistant;

public sealed record ConversationTurnRequest(
    string Role,
    string Content);