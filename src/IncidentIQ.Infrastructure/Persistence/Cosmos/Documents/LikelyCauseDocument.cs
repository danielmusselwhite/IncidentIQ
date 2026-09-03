using IncidentIQ.Application.Analyse;

namespace IncidentIQ.Infrastructure.Persistence.Cosmos.Documents;

/// <summary>
/// Cosmos persistence representation of a likely incident cause.
/// </summary>
internal sealed class LikelyCauseDocument
{
    public required string Cause { get; init; }

    public required double Confidence { get; init; }

    internal static LikelyCauseDocument FromApplication(LikelyCause likelyCause)
    {
        return new LikelyCauseDocument
        {
            Cause = likelyCause.Cause,
            Confidence = likelyCause.Confidence
        };
    }

    internal LikelyCause ToApplication() => new(Cause, Confidence);
}
