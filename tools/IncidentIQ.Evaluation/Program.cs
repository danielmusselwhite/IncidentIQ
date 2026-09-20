using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.Index;
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

    // Use the same Azure embedding implementation as the real application.
    var builder =
        Host.CreateApplicationBuilder(args);

    // Evaluation configuration is intentionally kept outside source control.
    builder.Configuration.AddUserSecrets(
        Assembly.GetExecutingAssembly(),
        optional: true);

    builder.Services.AddAzureEmbeddingDependencies(
        builder.Configuration);

    using var host =
        builder.Build();

    using var scope =
        host.Services.CreateScope();

    var embeddingGenerator =
        scope.ServiceProvider
            .GetRequiredService<IEmbeddingGenerator>();

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

    var runner =
        new RetrievalEvaluationRunner(
            embeddingGenerator,
            historicalRetriever,
            runbookRetriever);

    var results =
        new List<RetrievalEvaluationResult>();

    foreach (var evaluationCase in dataset.Cases)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Running {evaluationCase.Id}...");

        var result =
            await runner.RunAsync(
                evaluationCase);

        results.Add(result);

        RetrievalConsoleReporter.WriteCase(
            result);
    }

    #region evaluation report
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
        results,
        historicalIncidentCount: dataset.HistoricalIncidents.Count,
        runbookCount: dataset.Runbooks.Count,
        embeddingDeployment: embeddingDeployment,
        embeddingModel: embeddingModel,
        embeddingDimensions: embeddingDimensions);

    RetrievalConsoleReporter.WriteSummary(report);
    #endregion

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