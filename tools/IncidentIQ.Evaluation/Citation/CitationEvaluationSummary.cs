namespace IncidentIQ.Evaluation.Citations;

/// <summary>
/// Represents aggregate citation-grounding performance across
/// all generated evaluation responses.
/// </summary>
internal sealed record CitationEvaluationSummary(
    int CaseCount,
    int CitationCount,
    int ValidCitationCount,
    int InvalidCitationCount,
    double CitationValidity,
    int CasesWithInvalidCitations,
    int NoEvidencePassed,
    int NoEvidenceTotal);