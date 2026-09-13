namespace IncidentIQ.Application.Assistant.Conversation;

/// <summary>
/// Represents a previous conversational turn supplied to the Operational Assistant.
/// Conversation history provides continuity but is not treated as grounding evidence.
/// </summary>
public sealed record ConversationTurn(
    ConversationRole Role,
    string Content);