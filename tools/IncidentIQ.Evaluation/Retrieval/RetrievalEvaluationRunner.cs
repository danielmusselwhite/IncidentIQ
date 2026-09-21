using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Evaluation.Models;

namespace IncidentIQ.Evaluation.Retrieval;

/// <summary>
/// Runs controlled retrieval evaluation scenarios against the configured
/// IncidentIQ embedding and retrieval implementations.
/// </summary>
public sealed class RetrievalEvaluationRunner
{
    private const int HistoricalIncidentTopK = 3;
    private const int RunbookChunkTopK = 5;

    private static readonly int[] HistoricalMetricCutoffs =
        [1, 3];

    private static readonly int[] RunbookMetricCutoffs =
        [1, 3, 5];

    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IHistoricalIncidentRetriever _historicalIncidentRetriever;
    private readonly IRunbookChunkRetriever _runbookChunkRetriever;

    public RetrievalEvaluationRunner(
        IEmbeddingGenerator embeddingGenerator,
        IHistoricalIncidentRetriever historicalIncidentRetriever,
        IRunbookChunkRetriever runbookChunkRetriever)
    {
        _embeddingGenerator = embeddingGenerator;
        _historicalIncidentRetriever = historicalIncidentRetriever;
        _runbookChunkRetriever = runbookChunkRetriever;
    }

    public async Task<RetrievalEvaluationResult> RunAsync(
        EvaluationCase evaluationCase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluationCase);

        var retrievalInput =
            BuildRetrievalInput(evaluationCase);

        var embedding =
            await _embeddingGenerator.GenerateAsync(
                retrievalInput.QueryText,
                cancellationToken);

        var historicalTask =
            _historicalIncidentRetriever.RetrieveAsync(
                embedding,
                retrievalInput.Service,
                retrievalInput.Environment,
                HistoricalIncidentTopK,
                cancellationToken);

        var runbookTask =
            _runbookChunkRetriever.RetrieveAsync(
                embedding,
                retrievalInput.Service,
                RunbookChunkTopK,
                cancellationToken);

        await Task.WhenAll(
            historicalTask,
            runbookTask);

        var historicalMatches =
            await historicalTask;

        var runbookMatches =
            await runbookTask;

        return new RetrievalEvaluationResult(
            CaseId: evaluationCase.Id,
            CaseName: evaluationCase.Name,
            HistoricalIncidents:
                EvaluateHistoricalIncidents(
                    historicalMatches,
                    evaluationCase.ExpectedEvidence.HistoricalIncidentIds),
            Runbooks:
                EvaluateRunbooks(
                    runbookMatches,
                    evaluationCase.ExpectedEvidence.RunbookIds));
    }

    private static RetrievalInput BuildRetrievalInput(
        EvaluationCase evaluationCase)
    {
        return evaluationCase.ScenarioType switch
        {
            EvaluationScenarioType.OperationalQuestion =>
                BuildOperationalQuestionInput(evaluationCase),

            EvaluationScenarioType.IncidentAnalysis =>
                BuildIncidentAnalysisInput(evaluationCase),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported evaluation scenario type '{evaluationCase.ScenarioType}'.")
        };
    }

    private static RetrievalInput BuildOperationalQuestionInput(
        EvaluationCase evaluationCase)
    {
        var question =
            evaluationCase.Input.Question
            ?? throw new InvalidOperationException(
                $"Evaluation case '{evaluationCase.Id}' has no question.");

        return new RetrievalInput(
            QueryText: question.Trim(),
            Service: evaluationCase.Input.ServiceFilter,
            Environment: evaluationCase.Input.EnvironmentFilter);
    }

    private static RetrievalInput BuildIncidentAnalysisInput(
        EvaluationCase evaluationCase)
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

        return new RetrievalInput(
            QueryText: retrievalInput.QueryText,
            Service: retrievalInput.Service,
            Environment: retrievalInput.Environment);
    }

    private static RetrievalSourceEvaluation<RetrievedHistoricalIncident>
        EvaluateHistoricalIncidents(
            IReadOnlyList<Application.Incidents.HistoricalSearch.Retrieve.HistoricalIncidentMatch> matches,
            IReadOnlyList<Guid> expectedIds)
    {
        var expected =
            expectedIds.ToHashSet();

        var results =
            matches
                .Select((match, index) =>
                    new RetrievedHistoricalIncident(
                        IncidentId: match.IncidentId,
                        Rank: index + 1,
                        Distance: match.Distance,
                        IsExpected:
                            expected.Contains(match.IncidentId)))
                .ToList();

        var expectsNoEvidence =
            expected.Count == 0;

        return new RetrievalSourceEvaluation<RetrievedHistoricalIncident>(
            Results: results,
            Metrics:
                RetrievalMetricCalculator.Calculate(
                    matches
                        .Select(match => match.IncidentId)
                        .ToList(),
                    expectedIds,
                    HistoricalMetricCutoffs),
            ExpectsNoEvidence: expectsNoEvidence,
            NoEvidenceCorrect:
                expectsNoEvidence
                    ? results.Count == 0
                    : null);
    }

    private static RetrievalSourceEvaluation<RetrievedRunbookChunk>
        EvaluateRunbooks(
            IReadOnlyList<RunbookChunkMatch> matches,
            IReadOnlyList<Guid> expectedIds)
    {
        var expected =
            expectedIds.ToHashSet();

        var results =
            matches
                .Select((match, index) =>
                    new RetrievedRunbookChunk(
                        RunbookId: match.RunbookId,
                        ChunkIndex: match.ChunkIndex,
                        Rank: index + 1,
                        Distance: match.Distance,
                        IsExpected:
                            expected.Contains(match.RunbookId)))
                .ToList();

        var expectsNoEvidence =
            expected.Count == 0;

        return new RetrievalSourceEvaluation<RetrievedRunbookChunk>(
            Results: results,
            Metrics:
                RetrievalMetricCalculator.Calculate(
                    matches
                        .Select(match => match.RunbookId)
                        .ToList(),
                    expectedIds,
                    RunbookMetricCutoffs),
            ExpectsNoEvidence: expectsNoEvidence,
            NoEvidenceCorrect:
                expectsNoEvidence
                    ? results.Count == 0
                    : null);
    }

    private sealed record RetrievalInput(
        string QueryText,
        string? Service,
        string? Environment);
}