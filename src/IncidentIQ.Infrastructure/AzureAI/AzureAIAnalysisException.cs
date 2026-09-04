namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Represents a classified failure while generating an incident analysis through Azure AI.
/// </summary>
public sealed class AzureAIAnalysisException(
    AzureAIFailureCategory category,
    string message,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public AzureAIFailureCategory Category { get; } = category;
}