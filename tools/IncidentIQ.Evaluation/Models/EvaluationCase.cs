namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Defines one repeatable IncidentIQ AI evaluation scenario together with
/// the evidence and outcome expectations used to assess it.
/// </summary>
public sealed record EvaluationCase(
    string Id,
    string Name,
    EvaluationScenarioType ScenarioType,
    EvaluationInput Input,
    ExpectedEvidence ExpectedEvidence,
    ExpectedOutcome ExpectedOutcome,
    IReadOnlyList<string> Tags,
    string? Notes = null);