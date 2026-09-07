using System.ComponentModel.DataAnnotations;

namespace IncidentIQ.Infrastructure.AzureAI.Embedding;

/// <summary>
/// Configuration for the Azure OpenAI deployment used to generate Runbook embeddings.
/// </summary>
public sealed class AzureEmbeddingOptions
{
    public const string SectionName = "AzureAI:Embedding";

    [Required]
    public string DeploymentName { get; init; } = string.Empty;

    [Required]
    public string ModelName { get; init; } = string.Empty;

    [Range(1, 1536)]
    public int Dimensions { get; init; } = 1536;
}