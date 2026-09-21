namespace IncidentIQ.Evaluation.Citations;

/// <summary>
/// Calculates citation validity by comparing model-returned references
/// against the evidence identifiers supplied to the model.
/// </summary>
internal static class CitationMetricCalculator
{
    public static CitationEvaluationResult Calculate(
        string caseId,
        string caseName,
        IReadOnlyCollection<string> suppliedEvidenceReferences,
        IReadOnlyCollection<string> returnedEvidenceReferences,
        bool expectsNoEvidence)
    {
        ArgumentNullException.ThrowIfNull(suppliedEvidenceReferences);
        ArgumentNullException.ThrowIfNull(returnedEvidenceReferences);

        var supplied =
            suppliedEvidenceReferences
                .ToHashSet(StringComparer.Ordinal);

        var returned =
            returnedEvidenceReferences
                .Distinct(StringComparer.Ordinal)
                .ToList();

        var valid =
            returned
                .Where(supplied.Contains)
                .ToList();

        var invalid =
            returned
                .Where(reference => !supplied.Contains(reference))
                .ToList();

        var citationValidity =
            returned.Count == 0
                ? 1.0
                : (double)valid.Count / returned.Count;

        var noEvidenceCitationCorrect =
            !expectsNoEvidence || returned.Count == 0;

        return new CitationEvaluationResult(
            CaseId: caseId,
            CaseName: caseName,
            SuppliedEvidenceCount: supplied.Count,
            CitationCount: returned.Count,
            ValidCitationCount: valid.Count,
            InvalidCitationCount: invalid.Count,
            CitationValidity: citationValidity,
            ValidCitations: valid,
            InvalidCitations: invalid,
            ExpectsNoEvidence: expectsNoEvidence,
            NoEvidenceCitationCorrect: noEvidenceCitationCorrect);
    }
}