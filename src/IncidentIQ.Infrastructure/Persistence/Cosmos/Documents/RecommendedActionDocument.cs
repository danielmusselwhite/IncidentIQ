using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

internal sealed class RecommendedActionDocument
{
    public required string Action { get; init; }

    internal static RecommendedActionDocument FromDomain(RecommendedAction recommendedAction)
    {
        return new RecommendedActionDocument
        {
            Action = recommendedAction.Action
        };
    }
}
