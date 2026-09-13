namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Creates the durable evidence snapshot corresponding to an Incident analysis context.
/// Necessary for ensuring that the evidence used to generate an analysis is preserved for future reference, even if the underlying data changes.
/// Gives the permanent mapping from the transient analysis context to a durable evidence representation.
/// Eg RB-1 -> Real RunbookID, ChunkIndex, etc.
/// </summary>
public static class IncidentAnalysisEvidenceBuilder
{
    public static IncidentAnalysisEvidence Build(
        IncidentAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var historicalIncidents = context.HistoricalIncidents
            .Select((incident, index) => new HistoricalIncidentEvidence(
                ReferenceId: EvidenceReferenceId.HistoricalIncident(index),
                IncidentId: incident.IncidentId,
                Title: incident.Title,
                Description: incident.Description,
                Symptoms: incident.Symptoms,
                Service: incident.Service,
                Environment: incident.Environment,
                Severity: incident.Severity,
                CompletedAtUtc: incident.CompletedAtUtc,
                Distance: incident.Distance))
            .ToList();

        var runbookChunks = context.RunbookChunks
            .Select((chunk, index) => new RunbookChunkEvidence(
                ReferenceId: EvidenceReferenceId.RunbookChunk(index),
                RunbookId: chunk.RunbookId,
                ChunkIndex: chunk.ChunkIndex,
                Title: chunk.Title,
                Service: chunk.Service,
                Content: chunk.Content,
                Distance: chunk.Distance))
            .ToList();

        return new IncidentAnalysisEvidence(
            HistoricalIncidents: historicalIncidents,
            RunbookChunks: runbookChunks);
    }
}