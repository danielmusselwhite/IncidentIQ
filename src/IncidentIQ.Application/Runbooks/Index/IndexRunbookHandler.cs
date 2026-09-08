using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;

namespace IncidentIQ.Application.Runbooks.Index;

/// <summary>
/// Orchestrates the indexing of a Runbook into vectorised chunks.
/// </summary>
public sealed class IndexRunbookHandler(
    IRunbookRepository runbookRepository,
    RunbookChunker runbookChunker,
    IEmbeddingGenerator embeddingGenerator,
    IRunbookChunkStore runbookChunkStore)
{
    /// <summary>
    /// Loads the latest Runbook, splits its content into chunks, generates an
    /// embedding for each chunk and replaces the persisted vector index.
    /// </summary>
    /// <param name="command">The indexing request to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="RunbookNotFoundException">
    /// Thrown when the requested Runbook no longer exists.
    /// </exception>
    public async Task HandleAsync(
        IndexRunbookCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var runbook = await runbookRepository.GetByIdAsync(
            command.RunbookId,
            cancellationToken);

        if (runbook is null)
        {
            throw new RunbookNotFoundException(command.RunbookId);
        }

        var rawChunks = runbookChunker.Chunk(runbook.Content);

        var vectorisedChunks = new List<RunbookChunk>(
            rawChunks.Count);

        for (var chunkIndex = 0; chunkIndex < rawChunks.Count; chunkIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = rawChunks[chunkIndex];

            // Include useful Runbook metadata in the text sent to the embedding
            // model so semantically similar queries can benefit from context
            // such as the Runbook title and owning service.
            var textToEmbed = BuildEmbeddingText(
                runbook.Title,
                runbook.Service,
                content);

            var embedding = await embeddingGenerator.GenerateAsync(
                textToEmbed,
                cancellationToken);

            vectorisedChunks.Add(
                new RunbookChunk(
                    RunbookId: runbook.Id,
                    ChunkIndex: chunkIndex,
                    Content: content,
                    Title: runbook.Title,
                    Service: runbook.Service,
                    SourceUpdatedAtUtc: runbook.UpdatedAt,
                    Embedding: embedding));
        }

        await runbookChunkStore.ReplaceForRunbookAsync(
            runbook.Id,
            vectorisedChunks,
            cancellationToken);
    }

    /// <summary>
    /// Builds the text passed to the embedding model.
    /// </summary>
    private static string BuildEmbeddingText(
        string title,
        string service,
        string content)
    {
        return $"""
            Runbook: {title}
            Service: {service}

            {content}
            """;
    }
}