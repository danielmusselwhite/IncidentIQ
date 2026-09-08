using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;

namespace IncidentIQ.Application.Runbooks.RetrieveChunks;

/// <summary>
/// Handles the retrieval of Runbook chunks based on a natural-language query using semantic vector search.
/// </summary>
/// <param name="embeddingGenerator"></param>
/// <param name="runbookChunkRetriever"></param>
public sealed class RetrieveRunbookChunksHandler(
    IEmbeddingGenerator embeddingGenerator,
    IRunbookChunkRetriever runbookChunkRetriever,
    IValidator<RetrieveRunbookChunksQuery> validator)
{

    /// <summary>
    /// Handles the retrieval of runbook chunks based on the provided query.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<RunbookChunkMatch>> HandleAsync(
        RetrieveRunbookChunksQuery query,
        CancellationToken cancellationToken = default)
    {
        // Validation
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        // Generate an embedding for the natural-language query
        var queryEmbedding = await embeddingGenerator.GenerateAsync(query.Query, cancellationToken);

        // Retrieve the closest Runbook chunks
        return await runbookChunkRetriever.RetrieveAsync(queryEmbedding, query.Service, query.TopK, cancellationToken);
    }
}
