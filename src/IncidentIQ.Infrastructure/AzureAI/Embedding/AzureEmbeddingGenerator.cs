using IncidentIQ.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace IncidentIQ.Infrastructure.AzureAI.Embedding;

/// <summary>
/// Generates Runbook embeddings using the configured Azure OpenAI embedding deployment.
/// </summary>
public sealed class AzureEmbeddingGenerator(
    EmbeddingClient embeddingClient,
    IOptions<AzureEmbeddingOptions> options)
    : IEmbeddingGenerator
{
    private readonly AzureEmbeddingOptions _options = options.Value;

    /// <summary>
    /// Generates a numeric embedding vector for the supplied text.
    /// </summary>
    public async Task<IReadOnlyList<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // Explicitly request the same number of dimensions configured on the
        // Cosmos RunbookChunks vector policy.
        var generationOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.Dimensions
        };

        var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(
            text,
            generationOptions,
            cancellationToken);

        var embedding = embeddingResponse.Value;
        var vector = embedding.ToFloats();

        // Guard against configuration drift between Azure OpenAI and Cosmos.
        if (vector.Length != _options.Dimensions)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI returned an embedding with {vector.Length} dimensions, " +
                $"but {_options.Dimensions} dimensions were expected.");
        }

        return vector.ToArray();
    }
}