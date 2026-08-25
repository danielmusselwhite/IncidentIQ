using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Runbooks;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Infrastructure;

/// <summary>
/// Represents an in-memory implementation of the <see cref="IRunbookRepository"/>
/// interface for testing purposes.
/// </summary>
public sealed class InMemoryRunbookRepository : IRunbookRepository
{
    private readonly ConcurrentDictionary<Guid, Runbook> _runbooks = new();

    public Task CreateAsync(
        Runbook runbook,
        CancellationToken cancellationToken = default)
    {
        _runbooks[runbook.Id] = runbook;

        return Task.CompletedTask;
    }

    public Task<Runbook?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _runbooks.TryGetValue(id, out var runbook);

        return Task.FromResult(runbook);
    }

    public Task<IReadOnlyCollection<Runbook>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Runbook> runbooks = _runbooks.Values
            .OrderByDescending(runbook => runbook.UpdatedAt)
            .ToArray();

        return Task.FromResult(runbooks);
    }

    public Task UpdateAsync(
        Runbook runbook,
        CancellationToken cancellationToken = default)
    {
        _runbooks[runbook.Id] = runbook;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _runbooks.TryRemove(id, out _);

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _runbooks.Clear();
    }
}