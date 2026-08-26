using IncidentIQ.Application.Incidents.Analyse;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Provides the application abstraction for queuing Incident analysis work.
/// </summary>
public interface IIncidentAnalysisQueue
{
    Task EnqueueAsync(
        AnalyseIncidentCommand command,
        CancellationToken cancellationToken = default);
}