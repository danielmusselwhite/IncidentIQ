using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Evaluation.Retrieval;

public sealed class InMemoryRunbookChunkRetriever(
    EvaluationCorpusIndex index)
    : IRunbookChunkRetriever
{
    public Task<IReadOnlyList<RunbookChunkMatch>> RetrieveAsync(
        IReadOnlyList<float> queryEmbedding,
        string? service,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        cancellationToken.ThrowIfCancellationRequested();

        var matches =
            index.RunbookChunks
                .Where(chunk =>
                    string.IsNullOrWhiteSpace(service) ||
                    string.Equals(
                        chunk.Service,
                        service,
                        StringComparison.OrdinalIgnoreCase))
                .Select(chunk =>
                    new RunbookChunkMatch(
                        RunbookId:
                            chunk.RunbookId,
                        ChunkIndex:
                            chunk.ChunkIndex,
                        Title:
                            chunk.Title,
                        Service:
                            chunk.Service,
                        Content:
                            chunk.Content,
                        Distance:
                            CosineDistance.Calculate(
                                queryEmbedding,
                                chunk.Embedding)))
                .OrderBy(match => match.Distance)
                .Take(topK)
                .ToList();

        return Task.FromResult<
            IReadOnlyList<RunbookChunkMatch>>(
                matches);
    }
}