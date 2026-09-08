using FluentValidation;

namespace IncidentIQ.Application.Runbooks.RetrieveChunks;

/// <summary>
/// Validates requests to retrieve Runbook chunks using semantic search.
/// </summary>
public sealed class RetrieveRunbookChunksValidator
    : AbstractValidator<RetrieveRunbookChunksQuery>
{
    public RetrieveRunbookChunksValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty();

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 20);
    }
}