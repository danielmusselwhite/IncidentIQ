using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Api.Tests.Fakes;

public sealed class InMemoryRunbookChunkRetriever : IRunbookChunkRetriever
{
    private IReadOnlyList<RunbookChunkMatch> _results = [];

    public Task<IReadOnlyList<RunbookChunkMatch>> RetrieveAsync(
        IReadOnlyList<float> queryEmbedding,
        string? service,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var results = _results
            .Where(x =>
                string.IsNullOrWhiteSpace(service) ||
                x.Service == service)
            .Take(topK)
            .ToArray();

        return Task.FromResult<IReadOnlyList<RunbookChunkMatch>>(results);
    }

    public void SetResults(params RunbookChunkMatch[] results)
    {
        _results = results;
    }

    public void Clear()
    {
        _results = [];
    }
}