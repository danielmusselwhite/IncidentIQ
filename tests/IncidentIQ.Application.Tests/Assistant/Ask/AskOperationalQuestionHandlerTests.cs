using IncidentIQ.Application.Assistant.Ask;
using IncidentIQ.Application.Assistant.Generate;
using IncidentIQ.Application.Assistant.Grounding;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Incidents.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Domain.Incidents;
using Moq;

namespace IncidentIQ.Application.Tests.Assistant.Ask;

public sealed class AskOperationalQuestionHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsGroundedAnswerAndRetrievedEvidence()
    {
        // Arrange
        IReadOnlyList<float> embedding = [0.1f, 0.2f, 0.3f];

        var historicalIncident = new HistoricalIncidentMatch(
            IncidentId: Guid.NewGuid(),
            Title: "Payment gateway timeout",
            Description: "Payments failed because the external gateway timed out.",
            Symptoms: "HTTP 504 responses",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            CompletedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            Distance: 0.8);

        var runbookChunk = new RunbookChunkMatch(
            RunbookId: Guid.NewGuid(),
            ChunkIndex: 1,
            Title: "Payment Gateway Recovery",
            Service: "Payments",
            Content: "Check provider status and outbound gateway connectivity.",
            Distance: 0.7);

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

        var answeredAtUtc = DateTimeOffset.UtcNow;

        var generatedAnswer = new OperationalAnswer(
            Sections:
            [
                new OperationalAnswerSection(
                    Content:
                        "The symptoms are consistent with external payment gateway degradation.",
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
                It.Is<OperationalQuestionContext>(
                    context =>
                        context.Question ==
                            "Why are payment requests timing out?" &&
                        context.Service == "Payments" &&
                        context.Environment == "Production" &&
                        context.HistoricalIncidents.Count == 1 &&
                        context.RunbookChunks.Count == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedAnswer);

        var contextBuilder =
            new OperationalQuestionContextBuilder(
                embeddingGenerator.Object,
                historicalIncidentRetriever.Object,
                runbookChunkRetriever.Object);

        var sut = new AskOperationalQuestionHandler(
            contextBuilder,
            operationalAssistant.Object);

        var query = new AskOperationalQuestionQuery(
            Question: "Why are payment requests timing out?",
            Service: "Payments",
            Environment: "Production");

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        Assert.Equal(generatedAnswer, result.Answer);

        var returnedHistoricalIncident =
            Assert.Single(result.HistoricalIncidents);

        Assert.Equal(
            historicalIncident,
            returnedHistoricalIncident);

        var returnedRunbookChunk =
            Assert.Single(result.RunbookChunks);

        Assert.Equal(
            runbookChunk,
            returnedRunbookChunk);

        var section =
            Assert.Single(result.Answer.Sections);

        Assert.Equal(
            "The symptoms are consistent with external payment gateway degradation.",
            section.Content);

        Assert.Equal(
            ["HI-1", "RB-1"],
            section.EvidenceReferences);

        operationalAssistant.Verify(
            assistant => assistant.AnswerAsync(
                It.IsAny<OperationalQuestionContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAssistantReturnsUnknownEvidenceReference_Throws()
    {
        // Arrange
        IReadOnlyList<float> embedding = [0.1f, 0.2f, 0.3f];

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

        operationalAssistant
            .Setup(assistant => assistant.AnswerAsync(
                It.IsAny<OperationalQuestionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new OperationalAnswer(
                    Sections:
                    [
                        new OperationalAnswerSection(
                        Content: "Check the payment gateway.",
                        EvidenceReferences: ["HI-99"])
                    ],
                    Model: "test-model",
                    AnsweredAtUtc: DateTimeOffset.UtcNow));

        var contextBuilder =
            new OperationalQuestionContextBuilder(
                embeddingGenerator.Object,
                historicalIncidentRetriever.Object,
                runbookChunkRetriever.Object);

        var sut = new AskOperationalQuestionHandler(
            contextBuilder,
            operationalAssistant.Object);

        var query = new AskOperationalQuestionQuery(
            Question: "Why are payments failing?",
            Service: "Payments",
            Environment: "Production");

        // Act
        var action = () => sut.HandleAsync(query);

        // Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                action);

        Assert.Contains(
            "HI-99",
            exception.Message);
    }
}