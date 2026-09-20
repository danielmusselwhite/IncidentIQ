namespace IncidentIQ.Evaluation.Citations;

/// <summary>
/// Builds aggregate citation-grounding metrics from individual
/// evaluation case results.
/// </summary>
internal static class CitationEvaluationSummaryBuilder
{
    public static CitationEvaluationSummary Build(
        IReadOnlyList<CitationEvaluationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var citationCount =
            results.Sum(result => result.CitationCount);

        var validCitationCount =
            results.Sum(result => result.ValidCitationCount);

        var invalidCitationCount =
            results.Sum(result => result.InvalidCitationCount);

        var citationValidity =
            citationCount == 0
                ? 1.0
                : (double)validCitationCount / citationCount;

        var noEvidenceResults =
            results
                .Where(result => result.ExpectsNoEvidence)
                .ToList();

        return new CitationEvaluationSummary(
            CaseCount: results.Count,
            CitationCount: citationCount,
            ValidCitationCount: validCitationCount,
            InvalidCitationCount: invalidCitationCount,
            CitationValidity: citationValidity,
            CasesWithInvalidCitations:
                results.Count(
                    result => result.InvalidCitationCount > 0),
            NoEvidencePassed:
                noEvidenceResults.Count(
                    result => result.NoEvidenceCitationCorrect),
            NoEvidenceTotal:
                noEvidenceResults.Count);
    }
}