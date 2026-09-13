using IncidentIQ.Api.Assistant;
using IncidentIQ.Api.Contracts.Assistant;
using IncidentIQ.Application.Assistant.Ask;
using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Assistant.Grounding;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Domain.Incidents;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncidentIQ.Api.Tests.Assistant;

public sealed class AssistantApiTests
{
    [Fact]
    public async Task Ask_WhenGroundedEvidenceExists_ReturnsMappedResponse()
    {
        // Arrange
        IReadOnlyList<float> embedding =
        [
            0.1f,
            0.2f,
            0.3f
        ];

        var historicalIncident = new HistoricalIncidentMatch(
            IncidentId: Guid.NewGuid(),
            Title: "Payment gateway timeout",
            Description: "Payments failed because the external gateway timed out.",
            Symptoms: "HTTP 504 responses",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            CompletedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            Distance: 0.82);

        var runbookChunk = new RunbookChunkMatch(
            RunbookId: Guid.NewGuid(),
            ChunkIndex: 2,
            Title: "Payment Gateway Recovery",
            Service: "Payments",
            Content: "Check the provider status page and outbound gateway connectivity.",
            Distance: 0.76);

        var embeddingGenerator =
            new Mock<IEmbeddingGenerator>();

        var historicalIncidentRetriever =
            new Mock<IHistoricalIncidentRetriever>();

        var runbookChunkRetriever =
            new Mock<IRunbookChunkRetriever>();

        var operationalAssistant =
            new Mock<IOperationalAssistant>();

        embeddingGenerator
            .Setup(generator => generator.GenerateAsync(
                "Why are payment requests timing out?",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        historicalIncidentRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                "Payments",
                "Production",
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([historicalIncident]);

        runbookChunkRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                "Payments",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([runbookChunk]);

        var answeredAtUtc =
            DateTimeOffset.UtcNow;

        var answer = new OperationalAnswer(
            Sections:
            [
                new OperationalAnswerSection(
                    Content:
                        "The available evidence suggests external payment gateway degradation.",
                    EvidenceReferences:
                    [
                        "HI-1",
                        "RB-1"
                    ])
            ],
            Model: "test-model",
            AnsweredAtUtc: answeredAtUtc);

        operationalAssistant
            .Setup(assistant => assistant.AnswerAsync(
                It.IsAny<OperationalQuestionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

        var contextBuilder =
            new OperationalQuestionContextBuilder(
                embeddingGenerator.Object,
                historicalIncidentRetriever.Object,
                runbookChunkRetriever.Object);

        var handler =
            new AskOperationalQuestionHandler(
                contextBuilder,
                operationalAssistant.Object);

        var sut =
            new AssistantController(handler);

        var request =
            new AskOperationalQuestionRequest(
                Question:
                    "Why are payment requests timing out?",
                Service: "Payments",
                Environment: "Production",
                null);

        // Act
        var actionResult =
            await sut.Ask(
                request,
                CancellationToken.None);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(
                actionResult.Result);

        var response =
            Assert.IsType<OperationalAssistantResponse>(
                okResult.Value);

        Assert.Equal(
            "test-model",
            response.Model);

        Assert.Equal(
            answeredAtUtc,
            response.AnsweredAtUtc);

        var section =
            Assert.Single(response.Sections);

        Assert.Equal(
            "The available evidence suggests external payment gateway degradation.",
            section.Content);

        Assert.Equal(
            ["HI-1", "RB-1"],
            section.EvidenceReferences);

        var historicalEvidence =
            Assert.Single(
                response.Evidence.HistoricalIncidents);

        Assert.Equal(
            "HI-1",
            historicalEvidence.ReferenceId);

        Assert.Equal(
            historicalIncident.IncidentId,
            historicalEvidence.IncidentId);

        Assert.Equal(
            historicalIncident.Title,
            historicalEvidence.Title);

        Assert.Equal(
            historicalIncident.Description,
            historicalEvidence.Description);

        Assert.Equal(
            historicalIncident.Symptoms,
            historicalEvidence.Symptoms);

        Assert.Equal(
            historicalIncident.Service,
            historicalEvidence.Service);

        Assert.Equal(
            historicalIncident.Environment,
            historicalEvidence.Environment);

        Assert.Equal(
            historicalIncident.Severity.ToString(),
            historicalEvidence.Severity);

        Assert.Equal(
            historicalIncident.CompletedAtUtc,
            historicalEvidence.CompletedAtUtc);

        var runbookEvidence =
            Assert.Single(
                response.Evidence.RunbookChunks);

        Assert.Equal(
            "RB-1",
            runbookEvidence.ReferenceId);

        Assert.Equal(
            runbookChunk.RunbookId,
            runbookEvidence.RunbookId);

        Assert.Equal(
            runbookChunk.ChunkIndex,
            runbookEvidence.ChunkIndex);

        Assert.Equal(
            runbookChunk.Title,
            runbookEvidence.Title);

        Assert.Equal(
            runbookChunk.Service,
            runbookEvidence.Service);

        Assert.Equal(
            runbookChunk.Content,
            runbookEvidence.Content);
    }

    [Fact]
    public async Task Ask_WhenNoEvidenceIsRetrieved_ReturnsEmptyEvidenceCollections()
    {
        // Arrange
        IReadOnlyList<float> embedding =
        [
            0.1f,
            0.2f,
            0.3f
        ];

        var embeddingGenerator =
            new Mock<IEmbeddingGenerator>();

        var historicalIncidentRetriever =
            new Mock<IHistoricalIncidentRetriever>();

        var runbookChunkRetriever =
            new Mock<IRunbookChunkRetriever>();

        var operationalAssistant =
            new Mock<IOperationalAssistant>();

        embeddingGenerator
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        historicalIncidentRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        runbookChunkRetriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<IReadOnlyList<float>>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var answer = new OperationalAnswer(
            Sections:
            [
                new OperationalAnswerSection(
                    Content:
                        "No relevant operational evidence was retrieved for this question.",
                    EvidenceReferences: [])
            ],
            Model: "test-model",
            AnsweredAtUtc: DateTimeOffset.UtcNow);

        operationalAssistant
            .Setup(assistant => assistant.AnswerAsync(
                It.IsAny<OperationalQuestionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

        var contextBuilder =
            new OperationalQuestionContextBuilder(
                embeddingGenerator.Object,
                historicalIncidentRetriever.Object,
                runbookChunkRetriever.Object);

        var handler =
            new AskOperationalQuestionHandler(
                contextBuilder,
                operationalAssistant.Object);

        var sut =
            new AssistantController(handler);

        var request =
            new AskOperationalQuestionRequest(
                Question:
                    "Why is the service unhealthy?",
                Service: null,
                Environment: null,
                ConversationHistory: null);

        // Act
        var actionResult =
            await sut.Ask(
                request,
                CancellationToken.None);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(
                actionResult.Result);

        var response =
            Assert.IsType<OperationalAssistantResponse>(
                okResult.Value);

        var section =
            Assert.Single(response.Sections);

        Assert.Equal(
            "No relevant operational evidence was retrieved for this question.",
            section.Content);

        Assert.Empty(
            section.EvidenceReferences);

        Assert.Empty(
            response.Evidence.HistoricalIncidents);

        Assert.Empty(
            response.Evidence.RunbookChunks);
    }
}