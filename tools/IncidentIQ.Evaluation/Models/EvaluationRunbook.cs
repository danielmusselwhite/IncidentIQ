namespace IncidentIQ.Evaluation.Models;

/// <summary>
/// Represents a synthetic operational Runbook used as searchable guidance
/// during AI evaluation.
/// </summary>
public sealed record EvaluationRunbook(
    Guid RunbookId,
    string Title,
    string Description,
    string Service,
    string Content);