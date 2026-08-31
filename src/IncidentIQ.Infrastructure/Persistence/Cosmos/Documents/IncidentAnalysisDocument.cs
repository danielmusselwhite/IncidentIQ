using IncidentIQ.Application.Analyse;
using IncidentIQ.Domain.Incidents;
using System.Text.Json.Serialization;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a generated incident analysis.
/// Stored in the Incidents container using the owning Incident ID as the partition key.
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

    internal static IncidentAnalysisDocument FromDomain(
        IncidentAnalysisResult analysis,
        Incident incident)
    {
        return new IncidentAnalysisDocument
        {
            Id = $"analysis-{incident.Id}",
            IncidentId = incident.Id,
            Summary = analysis.Summary,
            LikelyCauses = analysis.LikelyCauses.Select(LikelyCauseDocument.FromDomain).ToList(),
            RecommendedActions = analysis.RecommendedActions.Select(RecommendedActionDocument.FromDomain).ToList(),
            Model = analysis.Model,
            AnalysedAtUtc = analysis.AnalysedAtUtc
        };
    }
}
