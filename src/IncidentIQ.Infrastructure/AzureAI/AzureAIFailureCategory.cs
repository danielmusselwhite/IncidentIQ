namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Identifies the broad reason an Azure AI analysis operation failed.
/// </summary>
public enum AzureAIFailureCategory
{
    Timeout,
    Throttled,
    ServiceFailure,
    ClientFailure,
    InvalidResponse,
    Unexpected
}