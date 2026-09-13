using IncidentIQ.Application.Common.Grounding;

namespace IncidentIQ.Application.Assistant.Grounding;

/// <summary>
/// Validates that evidence references returned by the Assistant correspond
/// to evidence supplied in the grounding context.
/// </summary>
public static class OperationalEvidenceReferenceValidator
{
    public static void Validate(
        IEnumerable<string> evidenceReferences,
        OperationalQuestionContext context)
    {
        ArgumentNullException.ThrowIfNull(evidenceReferences);
        ArgumentNullException.ThrowIfNull(context);

        var validReferences = new HashSet<string>(
            StringComparer.Ordinal);

        for (var i = 0; i < context.HistoricalIncidents.Count; i++)
        {
            validReferences.Add(EvidenceReferenceId.HistoricalIncident(i));
        }

        for (var i = 0; i < context.RunbookChunks.Count; i++)
        {
            validReferences.Add(EvidenceReferenceId.RunbookChunk(i));
        }

        var invalidReferences = evidenceReferences
            .Where(reference =>
                !validReferences.Contains(reference))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (invalidReferences.Count > 0)
        {
            throw new InvalidOperationException(
                $"Assistant returned evidence references that were not supplied: {string.Join(", ", invalidReferences)}.");
        }
    }
}