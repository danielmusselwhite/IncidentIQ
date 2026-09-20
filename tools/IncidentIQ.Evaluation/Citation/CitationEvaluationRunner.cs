using IncidentIQ.Application.Assistant.Conversation;
using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Assistant.Grounding;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Grounding;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Citations;

/// <summary>
/// Runs controlled citation and grounding evaluation scenarios against
/// the real configured AI generation implementations.
/// </summary>
internal sealed class CitationEvaluationRunner
{
    private const int HistoricalIncidentTopK = 3;
    private const int RunbookChunkTopK = 5;

    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IHistoricalIncidentRetriever _historicalIncidentRetriever;
    private readonly IRunbookChunkRetriever _runbookChunkRetriever;
    private readonly IOperationalAssistant _operationalAssistant;
    private readonly IIncidentAnalyzer _incidentAnalyzer;

    public CitationEvaluationRunner(
        IEmbeddingGenerator embeddingGenerator,
        IHistoricalIncidentRetriever historicalIncidentRetriever,
        IRunbookChunkRetriever runbookChunkRetriever,
        IOperationalAssistant operationalAssistant,
        IIncidentAnalyzer incidentAnalyzer)
    {
        _embeddingGenerator = embeddingGenerator;
        _historicalIncidentRetriever = historicalIncidentRetriever;
        _runbookChunkRetriever = runbookChunkRetriever;
        _operationalAssistant = operationalAssistant;
        _incidentAnalyzer = incidentAnalyzer;
    }

    public async Task<CitationEvaluationResult> RunAsync(
        EvaluationCase evaluationCase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluationCase);

        return evaluationCase.ScenarioType switch
        {
            EvaluationScenarioType.OperationalQuestion =>
                await RunOperationalQuestionAsync(
                    evaluationCase,
                    cancellationToken),

            EvaluationScenarioType.IncidentAnalysis =>
                await RunIncidentAnalysisAsync(
                    evaluationCase,
                    cancellationToken),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported evaluation scenario type '{evaluationCase.ScenarioType}'.")
        };
    }

    private async Task<CitationEvaluationResult> RunOperationalQuestionAsync(
        EvaluationCase evaluationCase,
        CancellationToken cancellationToken)
    {
        var question =
            evaluationCase.Input.Question
            ?? throw new InvalidOperationException(
                $"Evaluation case '{evaluationCase.Id}' has no question.");

        var evidence =
            await RetrieveEvidenceAsync(
                queryText: question.Trim(),
                service: evaluationCase.Input.ServiceFilter,
                environment: evaluationCase.Input.EnvironmentFilter,
                cancellationToken);

        var context =
            new OperationalQuestionContext(
                Question: question,
                Service: evaluationCase.Input.ServiceFilter,
                Environment: evaluationCase.Input.EnvironmentFilter,
                ConversationHistory: Array.Empty<ConversationTurn>(),
                HistoricalIncidents: evidence.HistoricalIncidents,
                RunbookChunks: evidence.RunbookChunks);

        var answer =
            await _operationalAssistant.AnswerAsync(
                context,
                cancellationToken);

        var returnedReferences =
            answer.Sections
                .SelectMany(section => section.EvidenceReferences)
                .Distinct(StringComparer.Ordinal)
                .ToList();

        return Calculate(
            evaluationCase,
            evidence,
            returnedReferences);
    }

    private async Task<CitationEvaluationResult> RunIncidentAnalysisAsync(
        EvaluationCase evaluationCase,
        CancellationToken cancellationToken)
    {
        var incident =
            evaluationCase.Input.Incident
            ?? throw new InvalidOperationException(
                $"Evaluation case '{evaluationCase.Id}' has no Incident input.");

        var analysisInput =
            new IncidentAnalysisInput(
                Title: incident.Title,
                Description: incident.Description,
                Service: incident.Service,
                Environment: incident.Environment,
                Symptoms: incident.Symptoms,
                Severity: incident.Severity);

        var retrievalInput =
            IncidentRetrievalInputBuilder.Build(
                analysisInput);

        var evidence =
            await RetrieveEvidenceAsync(
                retrievalInput.QueryText,
                retrievalInput.Service,
                retrievalInput.Environment,
                cancellationToken);

        var context =
            new IncidentAnalysisContext(
                Incident: analysisInput,
                HistoricalIncidents: evidence.HistoricalIncidents,
                RunbookChunks: evidence.RunbookChunks);

        var analysis =
            await _incidentAnalyzer.AnalyzeIncidentAsync(
                context,
                cancellationToken);

        var returnedReferences =
            analysis.LikelyCauses
                .SelectMany(cause => cause.EvidenceReferences)
                .Concat(
                    analysis.RecommendedActions
                        .SelectMany(action => action.EvidenceReferences))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        return Calculate(
            evaluationCase,
            evidence,
            returnedReferences);
    }

    private async Task<RetrievedEvidence> RetrieveEvidenceAsync(
        string queryText,
        string? service,
        string? environment,
        CancellationToken cancellationToken)
    {
        var embedding =
            await _embeddingGenerator.GenerateAsync(
                queryText,
                cancellationToken);

        var historicalTask =
            _historicalIncidentRetriever.RetrieveAsync(
                embedding,
                service,
                environment,
                HistoricalIncidentTopK,
                cancellationToken);

        var runbookTask =
            _runbookChunkRetriever.RetrieveAsync(
                embedding,
                service,
                RunbookChunkTopK,
                cancellationToken);

        await Task.WhenAll(
            historicalTask,
            runbookTask);

        return new RetrievedEvidence(
            HistoricalIncidents: await historicalTask,
            RunbookChunks: await runbookTask);
    }

    private static CitationEvaluationResult Calculate(
        EvaluationCase evaluationCase,
        RetrievedEvidence evidence,
        IReadOnlyCollection<string> returnedReferences)
    {
        var suppliedReferences =
            Enumerable
                .Range(
                    0,
                    evidence.HistoricalIncidents.Count)
                .Select(
                    EvidenceReferenceId.HistoricalIncident)
                .Concat(
                    Enumerable
                        .Range(
                            0,
                            evidence.RunbookChunks.Count)
                        .Select(
                            EvidenceReferenceId.RunbookChunk))
                .ToList();

        var expectsNoEvidence =
            evidence.HistoricalIncidents.Count == 0 &&
            evidence.RunbookChunks.Count == 0;

        return CitationMetricCalculator.Calculate(
            caseId: evaluationCase.Id,
            caseName: evaluationCase.Name,
            suppliedEvidenceReferences: suppliedReferences,
            returnedEvidenceReferences: returnedReferences,
            expectsNoEvidence: expectsNoEvidence);
    }

    private sealed record RetrievedEvidence(
        IReadOnlyList<Application.Incidents.HistoricalSearch.Retrieve.HistoricalIncidentMatch> HistoricalIncidents,
        IReadOnlyList<RunbookChunkMatch> RunbookChunks);
}