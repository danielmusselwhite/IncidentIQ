using IncidentIQ.Application.Analyse;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Persists a newly submitted Incident together with the analysis command that must be published for it.
/// </summary>
public interface IIncidentSubmissionStore
{
    /// <summary>
    /// Persists a newly submitted incident along with the associated analysis command.
    /// </summary>
    /// <param name="incident">The incident to persist.</param>
    /// <param name="analyseIncidentCommand">The analysis command associated with the incident.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The persisted <see cref="Incident"/>.</returns>
    Task<Incident> CreateAsync(
        Incident incident,
        AnalyseIncidentCommand analyseIncidentCommand,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a failed incident along with the associated retry analysis command.
    /// </summary>
    /// <param name="incident">The failed incident to persist.</param>
    /// <param name="retryAnalyseIncidentCommand">The retry analysis command associated with the incident.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The persisted <see cref="Incident"/>.</returns>
    Task<Incident> RetryAsync(
        Incident incident,
        AnalyseIncidentCommand retryAnalyseIncidentCommand,
        CancellationToken cancellationToken = default);
}