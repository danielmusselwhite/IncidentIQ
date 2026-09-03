using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;

namespace IncidentIQ.Application.Incidents.GetAnalysisById;

/// <summary>
/// Handles retrieval of a persisted analysis for a specific incident.
/// </summary>
/// <param name="incidentAnalysisReader">Provides read access to persisted incident analyses.</param>
public sealed class GetIncidentAnalysisByIdHandler(IIncidentAnalysisReader incidentAnalysisReader)
{
    /// <summary>
    /// Retrieves the analysis associated with the supplied incident ID.
    /// </summary>
    /// <param name="incidentId">The ID of the incident whose analysis should be returned.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The persisted incident analysis.</returns>
    /// <exception cref="IncidentAnalysisNotFoundException">Thrown when no analysis exists for the supplied incident ID.</exception>
    public async Task<IncidentAnalysisResult> HandleAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        var analysis = await incidentAnalysisReader.GetByIncidentIdAsync(incidentId, cancellationToken);

        return analysis ?? throw new IncidentAnalysisNotFoundException(incidentId);
    }
}
