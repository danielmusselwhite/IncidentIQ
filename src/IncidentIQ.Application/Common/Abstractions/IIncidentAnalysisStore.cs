using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Domain.Incidents;
namespace IncidentIQ.Application.Common.Abstractions;

/// <summary>
/// Defines the contract for incident analysis store operations.
/// </summary>
public interface IIncidentAnalysisStore
{
    /// <summary>
    /// Stores the analysis result for the specified incident.
    /// </summary>
    /// <param name="incident">The incident for which the analysis result is being stored.</param>
    /// <param name="analysis">The analysis result to store.</param>
    /// <param name="evidence">The evidence corresponding to the analysis result.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public Task StoreCompletedAnalysisAsync(Incident incident, IncidentAnalysisResult analysis, IncidentAnalysisEvidence evidence, CancellationToken cancellationToken = default);
}