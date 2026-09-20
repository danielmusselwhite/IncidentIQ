namespace IncidentIQ.Evaluation.Citations;

/// <summary>
/// Represents the citation-grounding result for one generated AI response.
/// </summary>
internal sealed record CitationEvaluationResult(
    string CaseId,
    string CaseName,
    int SuppliedEvidenceCount,
    int CitationCount,
    int ValidCitationCount,
    int InvalidCitationCount,
    double CitationValidity,
    IReadOnlyList<string> ValidCitations,
    IReadOnlyList<string> InvalidCitations,
    bool ExpectsNoEvidence,
    bool NoEvidenceCitationCorrect);