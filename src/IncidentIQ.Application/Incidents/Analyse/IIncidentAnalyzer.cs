using IncidentIQ.Application.Incidents.Analyse.Grounding;

namespace IncidentIQ.Application.Incidents.Analyse;

/// <summary>
/// Defines the application boundary. Something later will implement it, but Application doesn't know that implementation is Azure.
/// </summary>
public interface IIncidentAnalyzer
{

    /// <summary>
    /// Analyzes the given incident input and returns the analysis result.
    /// </summary>
    /// <param name="context">The incident analysis context containing all necessary information for analysis.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of the incident analysis.</returns>
    Task<IncidentAnalysisResult> AnalyzeIncidentAsync(IncidentAnalysisContext context, CancellationToken cancellationToken = default);
}
