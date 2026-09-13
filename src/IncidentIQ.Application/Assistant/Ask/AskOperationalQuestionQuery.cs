using IncidentIQ.Application.Assistant.Conversation;

namespace IncidentIQ.Application.Assistant.Ask;

/// <summary>
/// Represents an operational question submitted to the IncidentIQ Assistant.
/// Optional metadata filters can restrict retrieval to a relevant operational scope.
/// </summary>
public sealed record AskOperationalQuestionQuery(
    string Question,
    string? Service = null,
    string? Environment = null,
    IReadOnlyList<ConversationTurn>? ConversationHistory = null);