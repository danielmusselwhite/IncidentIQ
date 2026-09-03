using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Defines read access to persisted incident analysis results.
/// </summary>
public interface IIncidentAnalysisReader
{
    /// <summary>
    /// Retrieves the persisted analysis for an incident.
    /// </summary>
    /// <param name="incidentId">The ID of the incident whose analysis should be retrieved.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The analysis result, or <c>null</c> when no analysis has been persisted for the incident.</returns>
    Task<IncidentAnalysisResult?> GetByIncidentIdAsync(string incidentId, CancellationToken cancellationToken = default);
}
