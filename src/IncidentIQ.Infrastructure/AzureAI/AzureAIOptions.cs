using System.ComponentModel.DataAnnotations;

namespace IncidentIQ.Infrastructure.AzureAI;
    
/// <summary>
/// Configuration required to connect to the Azure AI model used for incident analysis.
/// Set in the application configuration (e.g., appsettings.json or environment variables).
/// In prod, the environment variables are set via the Bicep templates used for infrastructure deployment.
/// </summary>
public sealed class AzureAIOptions
{
    /// <summary>
    /// The section name in the application configuration for Azure AI options.
    /// </summary>
    public const string SectionName = "AzureAI";

    /// <summary>
    /// where the Azure OpenAI resource lives
    /// </summary>
    [Required]
    public required string Endpoint { get; init; }

    /// <summary>
    /// The name of the deployment within the Azure OpenAI resource.
    /// </summary>
    [Required]
    public required string DeploymentName { get; init; }

    /// <summary>
    /// The name of the model to use for incident analysis.
    /// </summary>
    [Required]
    public required string ModelName { get; init; }

    /// <summary>
    /// Maximum number of retry attempts performed by the Azure AI SDK
    /// before allowing the failure to propagate to the Worker.
    /// </summary>
    [Range(0, 5)]
    public int MaxRetries { get; init; } = 2;

    /// <summary>
    /// Maximum duration allowed for an individual network operation.
    /// </summary>
    [Range(10, 300)]
    public int NetworkTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Maximum duration allowed for the complete incident analysis operation.
    /// </summary>
    [Range(10, 300)]
    public int RequestTimeoutSeconds { get; init; } = 90;
}