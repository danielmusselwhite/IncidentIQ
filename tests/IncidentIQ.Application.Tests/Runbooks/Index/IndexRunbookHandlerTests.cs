using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Domain.Runbooks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks.Index;

public sealed class IndexRunbookHandlerTests
{
    private readonly Mock<IRunbookRepository> _runbookRepository = new();
    private readonly Mock<IEmbeddingGenerator> _embeddingGenerator = new();
    private readonly Mock<IRunbookChunkStore> _runbookChunkStore = new();

    [Fact]
    public async Task HandleAsync_WhenRunbookExists_ShouldGenerateAndStoreChunks()
    {
        // Arrange
        var runbook = Runbook.Create(
            "Payment Gateway Recovery",
            "Recovery steps for gateway failures.",
            "Payments",
            CreateLongContent());

        var command = CreateCommand(runbook);

        _runbookRepository
            .Setup(repository => repository.GetByIdAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        _embeddingGenerator
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmbedding());

        IReadOnlyCollection<RunbookChunk>? storedChunks = null;

        _runbookChunkStore
            .Setup(store => store.ReplaceForRunbookAsync(
                runbook.Id,
                It.IsAny<IReadOnlyCollection<RunbookChunk>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyCollection<RunbookChunk>, CancellationToken>(
                (_, chunks, _) => storedChunks = chunks)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(storedChunks);
        Assert.NotEmpty(storedChunks);

        Assert.All(
            storedChunks,
            chunk =>
            {
                Assert.Equal(runbook.Id, chunk.RunbookId);
                Assert.Equal(runbook.Title, chunk.Title);
                Assert.Equal(runbook.Service, chunk.Service);
                Assert.Equal(runbook.UpdatedAt, chunk.SourceUpdatedAtUtc);
                Assert.Equal(1536, chunk.Embedding.Count);
            });

        _embeddingGenerator.Verify(
            generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(storedChunks.Count));

        _runbookChunkStore.Verify(
            store => store.ReplaceForRunbookAsync(
                runbook.Id,
                It.IsAny<IReadOnlyCollection<RunbookChunk>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRunbookDoesNotExist_ShouldThrowRunbookNotFoundException()
    {
        // Arrange
        var runbookId = Guid.NewGuid();

        var command = new IndexRunbookCommand(
            CommandId: Guid.NewGuid(),
            RunbookId: runbookId,
            CorrelationId: Guid.NewGuid().ToString(),
            QueuedAtUtc: DateTime.UtcNow,
            SourceUpdatedAtUtc: DateTime.UtcNow);

        _runbookRepository
            .Setup(repository => repository.GetByIdAsync(
                runbookId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Runbook?)null);

        var handler = CreateHandler();

        // Act + Assert
        await Assert.ThrowsAsync<RunbookNotFoundException>(
            () => handler.HandleAsync(command));

        _embeddingGenerator.Verify(
            generator => generator.GenerateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _runbookChunkStore.Verify(
            store => store.ReplaceForRunbookAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<RunbookChunk>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private IndexRunbookHandler CreateHandler()
    {
        return new IndexRunbookHandler(
            _runbookRepository.Object,
            new RunbookChunker(),
            _embeddingGenerator.Object,
            _runbookChunkStore.Object);
    }

    private static IndexRunbookCommand CreateCommand(Runbook runbook)
    {
        return new IndexRunbookCommand(
            CommandId: Guid.NewGuid(),
            RunbookId: runbook.Id,
            CorrelationId: Guid.NewGuid().ToString(),
            QueuedAtUtc: DateTime.UtcNow,
            SourceUpdatedAtUtc: runbook.UpdatedAt);
    }

    private static IReadOnlyList<float> CreateEmbedding()
    {
        return new float[1536];
    }

    private static string CreateLongContent()
    {
        return string.Join(
            "\n\n",
            Enumerable.Range(1, 50)
                .Select(index =>
                    $"Recovery step {index}: Review payment gateway telemetry, downstream dependency health and application logs before continuing."));
    }
}