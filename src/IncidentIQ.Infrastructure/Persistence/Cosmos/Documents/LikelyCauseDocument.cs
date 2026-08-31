using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

internal sealed class LikelyCauseDocument
{
    public required string Cause { get; init; }

    public required double Confidence { get; init; }

    internal static LikelyCauseDocument FromDomain(LikelyCause likelyCause)
    {
        return new LikelyCauseDocument
        {
            Cause = likelyCause.Cause,
            Confidence = likelyCause.Confidence
        };
    }
}
