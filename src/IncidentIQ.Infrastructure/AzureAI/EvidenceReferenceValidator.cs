using IncidentIQ.Application.Incidents.Analyse.Grounding;

namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Validates that evidence references returned by Azure AI refer only to
/// evidence supplied in the corresponding grounded analysis context.
/// </summary>
internal static class EvidenceReferenceValidator
{
    public static void Validate(
        AzureIncidentAnalysisResponse response,
        IncidentAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(context);

        var validReferences = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < context.HistoricalIncidents.Count; i++)
            validReferences.Add(EvidenceReferenceId.HistoricalIncident(i));

        for (var i = 0; i < context.RunbookChunks.Count; i++)
            validReferences.Add(EvidenceReferenceId.RunbookChunk(i));

        var returnedReferences = response.LikelyCauses
            .SelectMany(cause => cause.EvidenceReferences)
            .Concat(response.RecommendedActions
                .SelectMany(action => action.EvidenceReferences));

        var invalidReferences = returnedReferences
            .Where(reference => !validReferences.Contains(reference))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (invalidReferences.Count > 0)
        {
            throw new InvalidOperationException(
                $"Azure AI returned evidence references that were not supplied: " +
                $"{string.Join(", ", invalidReferences)}.");
        }
    }
}