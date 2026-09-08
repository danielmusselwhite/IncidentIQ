using IncidentIQ.Application.Common.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace IncidentIQ.Infrastructure.AzureAI.Embedding;

/// <summary>
/// Deterministic local embedding generator used without Azure OpenAI.
/// These vectors are intended only for exercising the local ingestion pipeline.
/// </summary>
public sealed class DevelopmentDummyEmbeddingGenerator : IEmbeddingGenerator
{
    private const int Dimensions = 1536;

    public Task<IReadOnlyList<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();

        var embedding = new float[Dimensions];

        for (var i = 0; i < Dimensions; i++)
        {
            var input = Encoding.UTF8.GetBytes($"{text}:{i}");
            var hash = SHA256.HashData(input);

            var value = BitConverter.ToUInt32(hash, 0);

            // Normalise the deterministic integer into the range -1 to 1.
            embedding[i] = (value / (float)uint.MaxValue * 2f) - 1f;
        }

        return Task.FromResult<IReadOnlyList<float>>(embedding);
    }
}