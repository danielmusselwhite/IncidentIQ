using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.HistoricalSearch;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Converts the controlled synthetic corpus into the same vectorised
/// representations used by IncidentIQ retrieval.
/// </summary>
public sealed class EvaluationCorpusIndexer
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly RunbookChunker _runbookChunker;

    public EvaluationCorpusIndexer(
        IEmbeddingGenerator embeddingGenerator,
        RunbookChunker runbookChunker)
    {
        _embeddingGenerator = embeddingGenerator;
        _runbookChunker = runbookChunker;
    }

    public async Task<EvaluationCorpusIndex> BuildAsync(
        EvaluationDataset dataset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var historicalIncidents =
            await IndexHistoricalIncidentsAsync(
                dataset.HistoricalIncidents,
                cancellationToken);

        var runbookChunks =
            await IndexRunbooksAsync(
                dataset.Runbooks,
                cancellationToken);

        return new EvaluationCorpusIndex(
            HistoricalIncidents: historicalIncidents,
            RunbookChunks: runbookChunks);
    }

    private async Task<IReadOnlyList<HistoricalIncidentVector>>
        IndexHistoricalIncidentsAsync(
            IReadOnlyList<EvaluationHistoricalIncident> incidents,
            CancellationToken cancellationToken)
    {
        var indexed =
            new List<HistoricalIncidentVector>();

        foreach (var incident in incidents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var analysisInput =
                new IncidentAnalysisInput(
                    Title: incident.Title,
                    Description: incident.Description,
                    Service: incident.Service,
                    Environment: incident.Environment,
                    Symptoms: incident.Symptoms,
                    Severity: incident.Severity);

            // Reuse production retrieval formatting so the corpus and queries
            // use the same semantic representation.
            var retrievalInput =
                IncidentRetrievalInputBuilder.Build(
                    analysisInput);

            var embedding =
                await _embeddingGenerator.GenerateAsync(
                    retrievalInput.QueryText,
                    cancellationToken);

            indexed.Add(
                new HistoricalIncidentVector(
                    IncidentId:
                        incident.IncidentId.ToString(),
                    Title:
                        incident.Title,
                    Description:
                        incident.Description,
                    Symptoms:
                        incident.Symptoms,
                    Service:
                        incident.Service,
                    Environment:
                        incident.Environment,
                    Severity:
                        incident.Severity.ToString(),
                    CompletedAtUtc:
                        incident.CompletedAtUtc,
                    Embedding:
                        embedding));
        }

        return indexed;
    }

    /// <summary>
    /// Indexes the provided runbooks by chunking their content and generating embeddings for each chunk.
    /// </summary>
    /// <param name="runbooks">The runbooks to index.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of vectorised runbook chunks.</returns>
    private async Task<IReadOnlyList<RunbookChunk>>
        IndexRunbooksAsync(
            IReadOnlyList<EvaluationRunbook> runbooks,
            CancellationToken cancellationToken)
    {
        var indexed =
            new List<RunbookChunk>();

        foreach (var runbook in runbooks)
        {
            var chunks =
                _runbookChunker.Chunk(
                    runbook.Content);

            for (var index = 0;
                 index < chunks.Count;
                 index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content =
                    chunks[index];

                var embeddingText =
                    RunbookEmbeddingTextBuilder.Build(
                        runbook.Title,
                        runbook.Service,
                        content);

                var embedding =
                    await _embeddingGenerator.GenerateAsync(
                        embeddingText,
                        cancellationToken);

                indexed.Add(
                    new RunbookChunk(
                        RunbookId:
                            runbook.RunbookId,
                        ChunkIndex:
                            index,
                        Content:
                            content,
                        Title:
                            runbook.Title,
                        Service:
                            runbook.Service,
                        SourceUpdatedAtUtc:
                            DateTime.UnixEpoch,
                        Embedding:
                            embedding));
            }
        }

        return indexed;
    }
}