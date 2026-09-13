using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Application.Incidents.Analyse.Grounding;

/// <summary>
/// Builds the grounded context used for Incident analysis by retrieving
/// semantically relevant historical Incidents and Runbook chunks.
/// </summary>
/// <remarks>
/// A single embedding is generated from the current Incident and reused across
/// both retrieval sources. This keeps retrieval consistent and avoids making
/// multiple embedding requests for the same Incident.
///
/// Historical Incident evidence and Runbook evidence remain separate so that
/// downstream analysis can distinguish their provenance and later validate
/// evidence references produced by the AI.
/// </remarks>
public sealed class IncidentAnalysisContextBuilder
{
    // Keep the context relatively small so only the strongest historical
    // Incident matches are supplied to the model.
    private const int HistoricalIncidentTopK = 3;

    // Runbooks are chunked documents, so slightly more results are allowed
    // to provide enough operational guidance without excessively expanding
    // the AI prompt.
    private const int RunbookChunkTopK = 5;

    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IHistoricalIncidentRetriever _historicalIncidentRetriever;
    private readonly IRunbookChunkRetriever _runbookChunkRetriever;

    public IncidentAnalysisContextBuilder(
        IEmbeddingGenerator embeddingGenerator,
        IHistoricalIncidentRetriever historicalIncidentRetriever,
        IRunbookChunkRetriever runbookChunkRetriever)
    {
        _embeddingGenerator = embeddingGenerator;
        _historicalIncidentRetriever = historicalIncidentRetriever;
        _runbookChunkRetriever = runbookChunkRetriever;
    }

    /// <summary>
    /// Builds the complete retrieval context for an Incident before it is supplied to the configured Incident analyser.
    /// </summary>
    /// <param name="incident">
    /// The application-level representation of the Incident being analysed.
    /// </param>
    /// <param name="cancellationToken">
    /// Allows cancellation to propagate through embedding generation and retrieval.
    /// </param>
    /// <returns>
    /// The current Incident together with relevant historical Incident and
    /// Runbook evidence.
    /// </returns>
    public async Task<IncidentAnalysisContext> BuildAsync(
        IncidentAnalysisInput incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        // Build the semantic text used for vector retrieval while preserving service/environment separately for structured metadata filtering.
        var retrievalInput = IncidentRetrievalInputBuilder.Build(incident);

        // Both vector stores use the same embedding model, so one embedding can be reused for both historical Incident and Runbook retrieval.
        var embedding = await _embeddingGenerator.GenerateAsync(
            retrievalInput.QueryText,
            cancellationToken);

        // Start both independent Cosmos vector searches before awaiting either. This allows their I/O to run concurrently and reduces retrieval latency.
        var historicalIncidentsTask = _historicalIncidentRetriever.RetrieveAsync(
            embedding,
            retrievalInput.Service,
            retrievalInput.Environment,
            HistoricalIncidentTopK,
            cancellationToken);

        var runbookChunksTask = _runbookChunkRetriever.RetrieveAsync(
            embedding,
            retrievalInput.Service,
            RunbookChunkTopK,
            cancellationToken);

        await Task.WhenAll(historicalIncidentsTask, runbookChunksTask); // Await both retrievals to complete before constructing the context.

        // Keep both evidence sources distinct. Later stages will assign controlled evidence references so AI-produced citations can be validated.
        return new IncidentAnalysisContext(
            Incident: incident,
            HistoricalIncidents: await historicalIncidentsTask,
            RunbookChunks: await runbookChunksTask);
    }
}