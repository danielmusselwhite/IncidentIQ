namespace IncidentIQ.Application.Common.Grounding;

/// <summary>
/// Generates request-scoped identifiers used to reference grounding evidence
/// within an Incident analysis request and its model response.
/// </summary>
public static class EvidenceReferenceId
{
    public static string HistoricalIncident(int index) => $"HI-{index + 1}";

    public static string RunbookChunk(int index) => $"RB-{index + 1}";
}