namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Describes the concepts a strong grounded answer would normally contain.
///
/// These values support later human/quality evaluation and are not intended
/// to be matched using simple string equality.
/// </summary>
public sealed record ExpectedOutcome(
    IReadOnlyList<string> LikelyCauseConcepts,
    IReadOnlyList<string> RecommendedActionConcepts,
    bool ExpectUncertainty);