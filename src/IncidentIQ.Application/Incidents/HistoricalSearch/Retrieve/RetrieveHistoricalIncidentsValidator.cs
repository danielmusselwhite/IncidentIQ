using FluentValidation;

namespace IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;

/// <summary>
/// Validates historical Incident semantic retrieval requests.
/// </summary>
public sealed class RetrieveHistoricalIncidentsValidator : AbstractValidator<RetrieveHistoricalIncidentsQuery>
{
    public RetrieveHistoricalIncidentsValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty();

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 20);
    }
}