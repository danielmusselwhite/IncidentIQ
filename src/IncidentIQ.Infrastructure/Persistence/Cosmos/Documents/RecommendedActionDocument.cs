using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a recommended incident action.
/// </summary>
internal sealed class RecommendedActionDocument
{
    public required string Action { get; init; }

    internal static RecommendedActionDocument FromApplication(RecommendedAction recommendedAction)
    {
        return new RecommendedActionDocument
        {
            Action = recommendedAction.Action
        };
    }

    internal RecommendedAction ToApplication() => new(Action);
}
