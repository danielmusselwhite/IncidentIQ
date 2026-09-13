using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Domain.Incidents;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a generated incident analysis.
/// Stored in the Incidents container using the owning incident ID as the partition key.
/// </summary>
internal sealed class IncidentAnalysisDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("incidentId")]
    public required string IncidentId { get; init; }

    [JsonPropertyName("documentType")]
    public string DocumentType { get; init; } = "IncidentAnalysis";

    public required string Summary { get; init; }

    public required IReadOnlyList<LikelyCauseDocument> LikelyCauses { get; init; }

    public required IReadOnlyList<RecommendedActionDocument> RecommendedActions { get; init; }

    public required string Model { get; init; }

    public required DateTimeOffset AnalysedAtUtc { get; init; }

    public IReadOnlyList<HistoricalIncidentEvidenceDocument> HistoricalIncidentEvidence { get; init; } = [];

    public IReadOnlyList<RunbookChunkEvidenceDocument> RunbookEvidence { get; init; } = [];


    /// <summary>
    /// Creates the stable Cosmos document ID used for an incident analysis.
    /// The document ID is different from the partition key: the document ID is
    /// "analysis-{incidentId}", while the partition key remains the raw incident ID.
    /// </summary>
    internal static string CreateId(string incidentId) => $"analysis-{incidentId}";

    /// <summary>
    /// Maps the provider-independent Application result into its Cosmos representation.
    /// </summary>
    internal static IncidentAnalysisDocument FromApplication(IncidentAnalysisResult analysis, Incident incident, IncidentAnalysisEvidence evidence)
    {
        return new IncidentAnalysisDocument
        {
            Id = CreateId(incident.Id),
            IncidentId = incident.Id,
            Summary = analysis.Summary,
            LikelyCauses = analysis.LikelyCauses.Select(LikelyCauseDocument.FromApplication).ToList(),
            RecommendedActions = analysis.RecommendedActions.Select(RecommendedActionDocument.FromApplication).ToList(),
            Model = analysis.Model,
            AnalysedAtUtc = analysis.AnalysedAtUtc,
            HistoricalIncidentEvidence = evidence.HistoricalIncidents
                .Select(HistoricalIncidentEvidenceDocument.FromApplication)
                .ToList(),
            RunbookEvidence = evidence.RunbookChunks
                .Select(RunbookChunkEvidenceDocument.FromApplication)
                .ToList()
        };


    }

    /// <summary>
    /// Maps the Cosmos document back into the provider-independent Application result.
    /// </summary>
    internal GroundedIncidentAnalysis ToApplication()
    {
        var analysis = new IncidentAnalysisResult(
            Summary,
            LikelyCauses
                .Select(cause => cause.ToApplication())
                .ToList(),
            RecommendedActions
                .Select(action => action.ToApplication())
                .ToList(),
            Model,
            AnalysedAtUtc);

        var evidence = new IncidentAnalysisEvidence(
            HistoricalIncidents: HistoricalIncidentEvidence
                .Select(item => item.ToApplication())
                .ToList(),
            RunbookChunks: RunbookEvidence
                .Select(item => item.ToApplication())
                .ToList());

        return new GroundedIncidentAnalysis(
            Analysis: analysis,
            Evidence: evidence);
    }
}
