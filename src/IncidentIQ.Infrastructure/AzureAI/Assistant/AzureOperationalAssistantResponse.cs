using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.AzureAI.Assistant;

/// <summary>
/// Infrastructure-only representation of the structured response returned by
/// Azure OpenAI for an operational Assistant question.
/// </summary>
internal sealed class AzureOperationalAssistantResponse
{
    [JsonPropertyName("sections")]
    public required List<AzureOperationalAnswerSectionResponse> Sections { get; init; }

    /// <summary>
    /// Validates semantic constraints that are not guaranteed purely by the
    /// structured-output JSON schema.
    /// </summary>
    public void Validate()
    {
        if (Sections.Count == 0)
        {
            throw new InvalidOperationException(
                "Azure AI Assistant response must contain at least one section.");
        }

        foreach (var section in Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content))
            {
                throw new InvalidOperationException(
                    "Azure AI Assistant response section content cannot be empty.");
            }

            if (section.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException(
                    "Azure AI Assistant evidence references cannot contain empty values.");
            }
        }
    }
}

internal sealed class AzureOperationalAnswerSectionResponse
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required List<string> EvidenceReferences { get; init; }
}