using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using System.Collections.Concurrent;

namespace IncidentIQ.Api.Tests.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IIncidentAnalysisReader"/>
/// used by API integration tests.
/// </summary>
public sealed class InMemoryIncidentAnalysisReader : IIncidentAnalysisReader
{
    private readonly ConcurrentDictionary<string, GroundedIncidentAnalysis> _analyses = new();

    /// <summary>
    /// Retrieves a persisted analysis for an incident.
    /// </summary>
    public Task<GroundedIncidentAnalysis?> GetByIncidentIdAsync(
        string incidentId,
        CancellationToken cancellationToken = default)
    {
        _analyses.TryGetValue(incidentId, out var analysis);

        return Task.FromResult(analysis);
    }

    /// <summary>
    /// Adds or replaces an analysis result for a test incident.
    /// </summary>
    public void Set(string incidentId, GroundedIncidentAnalysis analysis)
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