using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Infrastructure;

/// <summary>
/// Represents an in-memory implementation of the <see cref="IIncidentRepository"/> interface for testing purposes.
/// </summary>
public sealed class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly ConcurrentDictionary<string, Incident> _incidents = new();

    public Task<Incident> CreateAsync(
        Incident incident,
        CancellationToken cancellationToken = default)
    {
        _incidents[incident.Id] = incident;

        return Task.FromResult(incident);
    }

    public Task<Incident?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _incidents.TryGetValue(id, out var incident);

        return Task.FromResult(incident);
    }

    public Task<IReadOnlyCollection<Incident>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Incident> incidents = _incidents.Values
            .OrderByDescending(incident => incident.CreatedAt)
            .ToArray();

        return Task.FromResult(incidents);
    }

    public void Clear()
    {
        _incidents.Clear();
    }
}