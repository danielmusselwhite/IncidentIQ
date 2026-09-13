using IncidentIQ.Application.Incidents.Analyse.Grounding;

namespace IncidentIQ.Api.Contracts.Incidents;

/// <summary>
/// API response returned when a persisted grounded Incident analysis is retrieved.
/// </summary>
public sealed record IncidentAnalysisResponse(
    string Summary,
    IReadOnlyList<LikelyCauseResponse> LikelyCauses,
    IReadOnlyList<RecommendedActionResponse> RecommendedActions,
    IncidentAnalysisEvidenceResponse Evidence,
    string Model,
    DateTimeOffset AnalysedAtUtc)
{
    /// <summary>
    /// Maps the persisted grounded Application analysis into the HTTP response contract.
    /// </summary>
    public static IncidentAnalysisResponse FromApplication(
        GroundedIncidentAnalysis groundedAnalysis)
    {
        var analysis = groundedAnalysis.Analysis;
        var evidence = groundedAnalysis.Evidence;

        return new IncidentAnalysisResponse(
            Summary: analysis.Summary,
            LikelyCauses: analysis.LikelyCauses
                .Select(cause => new LikelyCauseResponse(
                    cause.Cause,
                    cause.Confidence,
                    cause.EvidenceReferences))
                .ToList(),
            RecommendedActions: analysis.RecommendedActions
                .Select(action => new RecommendedActionResponse(
                    action.Action,
                    action.EvidenceReferences))
                .ToList(),
            Evidence: IncidentAnalysisEvidenceResponse.FromApplication(evidence),
            Model: analysis.Model,
            AnalysedAtUtc: analysis.AnalysedAtUtc);
    }
}

public sealed record LikelyCauseResponse(
    string Cause,
    double Confidence,
    IReadOnlyList<string> EvidenceReferences);

public sealed record RecommendedActionResponse(
    string Action,
    IReadOnlyList<string> EvidenceReferences);

public sealed record IncidentAnalysisEvidenceResponse(
    IReadOnlyList<HistoricalIncidentEvidenceResponse> HistoricalIncidents,
    IReadOnlyList<RunbookChunkEvidenceResponse> RunbookChunks)
{
    public static IncidentAnalysisEvidenceResponse FromApplication(
        IncidentAnalysisEvidence evidence)
    {
        return new IncidentAnalysisEvidenceResponse(
            HistoricalIncidents: evidence.HistoricalIncidents
                .Select(item => new HistoricalIncidentEvidenceResponse(
                    item.ReferenceId,
                    item.IncidentId,
                    item.Title,
                    item.Description,
                    item.Symptoms,
                    item.Service,
                    item.Environment,
                    item.Severity.ToString(),
                    item.CompletedAtUtc,
                    item.Distance))
                .ToList(),
            RunbookChunks: evidence.RunbookChunks
                .Select(item => new RunbookChunkEvidenceResponse(
                    item.ReferenceId,
                    item.RunbookId,
                    item.ChunkIndex,
                    item.Title,
                    item.Service,
                    item.Content,
                    item.Distance))
                .ToList());
    }
}

public sealed record HistoricalIncidentEvidenceResponse(
    string ReferenceId,
    Guid IncidentId,
    string Title,
    string Description,
    string? Symptoms,
    string Service,
    string Environment,
    string Severity,
    DateTimeOffset CompletedAtUtc,
    double Distance);

public sealed record RunbookChunkEvidenceResponse(
    string ReferenceId,
    Guid RunbookId,
    int ChunkIndex,
    string Title,
    string Service,
    string Content,
    double Distance);