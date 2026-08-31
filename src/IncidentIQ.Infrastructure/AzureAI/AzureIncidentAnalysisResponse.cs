using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Infrastructure-only representation of the structured JSON returned by Azure AI.
/// This type deliberately does not escape Infrastructure; it is validated and then
/// mapped to IncidentAnalysisResult from the Application layer.
/// </summary>
internal sealed class AzureIncidentAnalysisResponse
{
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("likelyCauses")]
    public required List<AzureLikelyCauseResponse> LikelyCauses { get; init; }

    [JsonPropertyName("recommendedActions")]
    public required List<AzureRecommendedActionResponse> RecommendedActions { get; init; }

    /// <summary>
    /// Performs application-side validation even though Structured Outputs already
    /// constrain the JSON shape. This protects the system from semantically invalid
    /// values such as blank strings or confidence values outside 0..1.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Summary))
            throw new InvalidOperationException("Azure AI analysis summary cannot be empty.");

        if (LikelyCauses.Count == 0)
            throw new InvalidOperationException("Azure AI analysis must contain at least one likely cause.");

        if (RecommendedActions.Count == 0)
            throw new InvalidOperationException("Azure AI analysis must contain at least one recommended action.");

        foreach (var cause in LikelyCauses)
        {
            if (string.IsNullOrWhiteSpace(cause.Cause))
                throw new InvalidOperationException("Azure AI likely cause cannot be empty.");

            if (cause.Confidence is < 0 or > 1)
                throw new InvalidOperationException(
                    $"Azure AI likely cause confidence must be between 0 and 1. Received: {cause.Confidence}.");
        }

        foreach (var action in RecommendedActions)
        {
            if (string.IsNullOrWhiteSpace(action.Action))
                throw new InvalidOperationException("Azure AI recommended action cannot be empty.");
        }
    }
}

internal sealed class AzureLikelyCauseResponse
{
    [JsonPropertyName("cause")]
    public required string Cause { get; init; }

    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }
}

internal sealed class AzureRecommendedActionResponse
{
    [JsonPropertyName("action")]
    public required string Action { get; init; }
}
