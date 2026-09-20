using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Evaluation.Citations;
using IncidentIQ.Evaluation.Data;
using IncidentIQ.Evaluation.Models;
using IncidentIQ.Evaluation.Reporting;
using IncidentIQ.Evaluation.Retrieval;
using IncidentIQ.Infrastructure.AzureAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

var dataDirectory =
    Path.Combine(
        AppContext.BaseDirectory,
        "Data");

try
{
    var dataset =
        await EvaluationDatasetLoader.LoadAsync(
            dataDirectory);

    Console.WriteLine(
        "IncidentIQ evaluation dataset loaded successfully.");

    Console.WriteLine(
        $"Historical Incidents: {dataset.HistoricalIncidents.Count}");

    Console.WriteLine(
        $"Runbooks: {dataset.Runbooks.Count}");

    Console.WriteLine(
        $"Evaluation cases: {dataset.Cases.Count}");

    // Use the same Azure AI implementations as the real application.
    var builder =
        Host.CreateApplicationBuilder(args);

    // Evaluation configuration is intentionally kept outside source control.
    builder.Configuration.AddUserSecrets(
        Assembly.GetExecutingAssembly(),
        optional: true);

    builder.Services.AddAzureAIDependencies(
        builder.Configuration);

    using var host =
        builder.Build();

    using var scope =
        host.Services.CreateScope();

    var embeddingGenerator =
        scope.ServiceProvider
            .GetRequiredService<IEmbeddingGenerator>();

    var operationalAssistant =
        scope.ServiceProvider
            .GetRequiredService<IOperationalAssistant>();

    var incidentAnalyzer =
        scope.ServiceProvider
            .GetRequiredService<IIncidentAnalyzer>();

    Console.WriteLine();
    Console.WriteLine(
        "Vectorising controlled evaluation corpus...");

    var corpusIndexer =
        new EvaluationCorpusIndexer(
            embeddingGenerator,
            new RunbookChunker());

    var corpusIndex =
        await corpusIndexer.BuildAsync(
            dataset);

    Console.WriteLine(
        $"Indexed historical Incidents: " +
        $"{corpusIndex.HistoricalIncidents.Count}");

    Console.WriteLine(
        $"Indexed Runbook chunks: " +
        $"{corpusIndex.RunbookChunks.Count}");

    var historicalRetriever =
        new InMemoryHistoricalIncidentRetriever(
            corpusIndex);

    var runbookRetriever =
        new InMemoryRunbookChunkRetriever(
            corpusIndex);

    var retrievalRunner =
        new RetrievalEvaluationRunner(
            embeddingGenerator,
            historicalRetriever,
            runbookRetriever);

    var citationRunner =
        new CitationEvaluationRunner(
            embeddingGenerator,
            historicalRetriever,
            runbookRetriever,
            operationalAssistant,
            incidentAnalyzer);

    var retrievalResults =
        new List<RetrievalEvaluationResult>();

    foreach (var evaluationCase in dataset.Cases)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Running {evaluationCase.Id}...");

        var result =
            await retrievalRunner.RunAsync(
                evaluationCase);

        retrievalResults.Add(
            result);

        RetrievalConsoleReporter.WriteCase(
            result);
    }

    Console.WriteLine();
    Console.WriteLine(
        "Running citation and grounding evaluation...");

    var citationResults =
        new List<CitationEvaluationResult>();

    foreach (var evaluationCase in dataset.Cases)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Generating grounded response for {evaluationCase.Id}...");

        var citationResult =
            await citationRunner.RunAsync(
                evaluationCase);

        citationResults.Add(
            citationResult);

        Console.WriteLine(
            $"  Supplied evidence: {citationResult.SuppliedEvidenceCount}");

        Console.WriteLine(
            $"  Citations: {citationResult.CitationCount}");

        Console.WriteLine(
            $"  Valid: {citationResult.ValidCitationCount}");

        Console.WriteLine(
            $"  Invalid: {citationResult.InvalidCitationCount}");

        Console.WriteLine(
            $"  Citation validity: {citationResult.CitationValidity:F3}");

        if (citationResult.ExpectsNoEvidence)
        {
            Console.WriteLine(
                $"  No-evidence citation behaviour: " +
                $"{(citationResult.NoEvidenceCitationCorrect ? "PASS" : "FAIL")}");
        }

        if (citationResult.InvalidCitations.Count > 0)
        {
            Console.WriteLine(
                $"  Invalid references: " +
                $"{string.Join(", ", citationResult.InvalidCitations)}");
        }
    }

    var embeddingDeployment =
        builder.Configuration["AzureAI:Embedding:DeploymentName"]
        ?? throw new InvalidOperationException(
            "AzureAI:Embedding:DeploymentName is not configured.");

    var embeddingModel =
        builder.Configuration["AzureAI:Embedding:ModelName"]
        ?? throw new InvalidOperationException(
            "AzureAI:Embedding:ModelName is not configured.");

    var embeddingDimensions =
        builder.Configuration.GetValue<int?>(
            "AzureAI:Embedding:Dimensions")
        ?? throw new InvalidOperationException(
            "AzureAI:Embedding:Dimensions is not configured.");

    var report =
        EvaluationReportBuilder.Build(
            retrievalResults,
            citationResults,
            historicalIncidentCount: dataset.HistoricalIncidents.Count,
            runbookCount: dataset.Runbooks.Count,
            embeddingDeployment: embeddingDeployment,
            embeddingModel: embeddingModel,
            embeddingDimensions: embeddingDimensions);

    // Stage 13B retrieval summary.
    RetrievalConsoleReporter.WriteSummary(
        report);

    // Stage 13C citation and grounding summary.
    Console.WriteLine();
    Console.WriteLine(
        "========================================");

    Console.WriteLine(
        "Citation & Grounding Evaluation Summary");

    Console.WriteLine(
        "========================================");

    Console.WriteLine(
        $"Cases evaluated: {report.CitationEvaluation.CaseCount}");

    Console.WriteLine(
        $"Citations returned: {report.CitationEvaluation.CitationCount}");

    Console.WriteLine(
        $"Valid citations: {report.CitationEvaluation.ValidCitationCount}");

    Console.WriteLine(
        $"Invalid citations: {report.CitationEvaluation.InvalidCitationCount}");

    Console.WriteLine(
        $"Citation validity: {report.CitationEvaluation.CitationValidity:F3}");

    Console.WriteLine(
        $"Cases with invalid citations: " +
        $"{report.CitationEvaluation.CasesWithInvalidCitations}");

    if (report.CitationEvaluation.NoEvidenceTotal > 0)
    {
        Console.WriteLine(
            $"No-evidence citation checks: " +
            $"{report.CitationEvaluation.NoEvidencePassed}/" +
            $"{report.CitationEvaluation.NoEvidenceTotal} passed");
    }

    var resultsDirectory =
        Path.Combine(
            AppContext.BaseDirectory,
            "Results");

    var reportPath =
        await EvaluationReportWriter.WriteAsync(
            report,
            resultsDirectory);

    Console.WriteLine();
    Console.WriteLine(
        $"Machine-readable report written to: {reportPath}");
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Evaluation failed:");

    Console.Error.WriteLine(
        exception);

    Environment.ExitCode = 1;
}