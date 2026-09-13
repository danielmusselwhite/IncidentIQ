using IncidentIQ.Application.Common.Grounding;

namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Validates that evidence references returned from an analysis refer only
/// to evidence that was supplied in the corresponding grounding context.
/// </summary>
public static class EvidenceReferenceValidator
{
    /// <summary>
    /// Validates that the given evidence references are all valid for the given context.
    /// </summary>
    /// <param name="evidenceReferences">The evidence references to validate.</param>
    /// <param name="context">The incident analysis context containing the valid evidence references.</param>
    public static void Validate(
        IEnumerable<string> evidenceReferences,
        IncidentAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(evidenceReferences);
        ArgumentNullException.ThrowIfNull(context);

        var validReferences = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < context.HistoricalIncidents.Count; i++)
            validReferences.Add(EvidenceReferenceId.HistoricalIncident(i));

        for (var i = 0; i < context.RunbookChunks.Count; i++)
            validReferences.Add(EvidenceReferenceId.RunbookChunk(i));

        var invalidReferences = evidenceReferences
            .Where(reference => !validReferences.Contains(reference))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (invalidReferences.Count > 0)
        {
            throw new InvalidOperationException(
                $"Analysis returned evidence references that were not supplied: " +
                $"{string.Join(", ", invalidReferences)}.");
        }
    }
}