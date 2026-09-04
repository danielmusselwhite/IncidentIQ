using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Common.Abstractions;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IIncidentAnalysisReader"/>
/// used by API integration tests.
/// </summary>
public sealed class InMemoryIncidentAnalysisReader : IIncidentAnalysisReader
{
    private readonly ConcurrentDictionary<string, IncidentAnalysisResult> _analyses = new();

    /// <summary>
    /// Retrieves a persisted analysis for an incident.
    /// </summary>
    public Task<IncidentAnalysisResult?> GetByIncidentIdAsync(
        string incidentId,
        CancellationToken cancellationToken = default)
    {
        _analyses.TryGetValue(incidentId, out var analysis);

        return Task.FromResult(analysis);
    }

    /// <summary>
    /// Adds or replaces an analysis result for a test incident.
    /// </summary>
    public void Set(string incidentId, IncidentAnalysisResult analysis)
    {
        _analyses[incidentId] = analysis;
    }

    /// <summary>
    /// Removes all persisted test analyses.
    /// </summary>
    public void Clear()
    {
        _analyses.Clear();
    }
}