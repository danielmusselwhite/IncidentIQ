using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.Index;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Fakes;

/// <summary>
/// In-memory Runbook chunk store used by API integration tests.
/// </summary>
public sealed class InMemoryRunbookChunkStore : IRunbookChunkStore
{
    private readonly ConcurrentDictionary<
        Guid,
        IReadOnlyCollection<RunbookChunk>> _chunks = new();

    public Task ReplaceForRunbookAsync(
        Guid runbookId,
        IReadOnlyCollection<RunbookChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            _chunks.TryRemove(runbookId, out _);
        }
        else
        {
            _chunks[runbookId] = chunks.ToArray();
        }

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<RunbookChunk> GetByRunbookId(Guid runbookId)
    {
        return _chunks.TryGetValue(runbookId, out var chunks)
            ? chunks
            : [];
    }

    public void Clear()
    {
        _chunks.Clear();
    }
}