using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;

namespace IncidentIQ.Application.Assistant.Grounding;

/// <summary>
/// Retrieves the operational evidence used to ground an Assistant response.
/// </summary>
public sealed class OperationalQuestionContextBuilder
{
    private const int HistoricalIncidentTopK = 3;
    private const int RunbookChunkTopK = 5;

    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IHistoricalIncidentRetriever _historicalIncidentRetriever;
    private readonly IRunbookChunkRetriever _runbookChunkRetriever;

    public OperationalQuestionContextBuilder(
        IEmbeddingGenerator embeddingGenerator,
        IHistoricalIncidentRetriever historicalIncidentRetriever,
        IRunbookChunkRetriever runbookChunkRetriever)
    {
        _embeddingGenerator = embeddingGenerator;
        _historicalIncidentRetriever = historicalIncidentRetriever;
        _runbookChunkRetriever = runbookChunkRetriever;
    }

    public async Task<OperationalQuestionContext> BuildAsync(
    string question,
    string? service,
    string? environment,
    IReadOnlyList<ConversationTurn>? conversationHistory = null,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        var trimmedQuestion = question.Trim();

        // Only the current question drives semantic retrieval.
        var embedding = await _embeddingGenerator.GenerateAsync(
            trimmedQuestion,
            cancellationToken);

        var historicalIncidentsTask =
            _historicalIncidentRetriever.RetrieveAsync(
                embedding,
                service,
                environment,
                HistoricalIncidentTopK,
                cancellationToken);

        var runbookChunksTask =
            _runbookChunkRetriever.RetrieveAsync(
                embedding,
                service,
                RunbookChunkTopK,
                cancellationToken);

        await Task.WhenAll(
            historicalIncidentsTask,
            runbookChunksTask);

        return new OperationalQuestionContext(
            Question: trimmedQuestion,
            Service: service,
            Environment: environment,
            ConversationHistory:
                conversationHistory ?? [],
            HistoricalIncidents:
                await historicalIncidentsTask,
            RunbookChunks:
                await runbookChunksTask);
    }
}