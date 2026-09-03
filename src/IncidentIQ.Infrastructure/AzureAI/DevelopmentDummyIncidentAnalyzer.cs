using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Provides deterministic incident analysis for local development without requiring Azure OpenAI.
/// </summary>
public sealed class DevelopmentDummyIncidentAnalyzer : IIncidentAnalyzer
{
    /// <summary>
    /// Returns a predictable analysis result so the complete asynchronous workflow
    /// and frontend can be developed locally without making external AI calls.
    /// </summary>
    public Task<IncidentAnalysisResult> AnalyzeIncidentAsync(
        IncidentAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        var result = new IncidentAnalysisResult(
            Summary: $"Development analysis for '{input.Title}'. The incident requires investigation based on the supplied symptoms and service context.",
            LikelyCauses:
            [
                new LikelyCause(
                    "A recent application or configuration change may have introduced unexpected behaviour.",
                    0.75),
                new LikelyCause(
                    "A downstream dependency or resource constraint may be contributing to the incident.",
                    0.55)
            ],
            RecommendedActions:
            [
                new RecommendedAction("Review recent deployments and configuration changes for the affected service."),
                new RecommendedAction("Inspect application logs and service health metrics around the time of the incident."),
                new RecommendedAction("Check dependent services for elevated latency or failures.")
            ],
            Model: "development-analyzer",
            AnalysedAtUtc: DateTimeOffset.UtcNow);

        return Task.FromResult(result);
    }
}