using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Api.Contracts.Incidents;

/// <summary>
/// API response returned when a persisted incident analysis is retrieved.
/// </summary>
public sealed record IncidentAnalysisResponse(
    string Summary,
    IReadOnlyList<LikelyCauseResponse> LikelyCauses,
    IReadOnlyList<RecommendedActionResponse> RecommendedActions,
    string Model,
    DateTimeOffset AnalysedAtUtc)
{
    /// <summary>
    /// Maps the Application analysis result into the HTTP response contract exposed by the API.
    /// </summary>
    public static IncidentAnalysisResponse FromApplication(IncidentAnalysisResult analysis)
    {
        return new IncidentAnalysisResponse(
            analysis.Summary,
            analysis.LikelyCauses.Select(cause => new LikelyCauseResponse(cause.Cause, cause.Confidence)).ToList(),
            analysis.RecommendedActions.Select(action => new RecommendedActionResponse(action.Action)).ToList(),
            analysis.Model,
            analysis.AnalysedAtUtc);
    }
}

/// <summary>
/// API representation of a possible incident cause and the model's confidence in that hypothesis.
/// </summary>
public sealed record LikelyCauseResponse(string Cause, double Confidence);

/// <summary>
/// API representation of an action recommended by the incident analysis.
/// </summary>
public sealed record RecommendedActionResponse(string Action);
