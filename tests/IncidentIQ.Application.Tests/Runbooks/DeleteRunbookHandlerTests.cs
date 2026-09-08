using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Runbooks.Delete;
using IncidentIQ.Application.Runbooks.Index;
using IncidentIQ.Domain.Runbooks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks;

public sealed class DeleteRunbookHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRunbookExists_ShouldDeleteChunksThenDeleteRunbook()
    {
        var runbook = Runbook.Create(
            "Title",
            "Description",
            "Service",
            "Content");

        var repository = new Mock<IRunbookRepository>();
        var chunkStore = new Mock<IRunbookChunkStore>();

        repository
            .Setup(x => x.GetByIdAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        chunkStore
            .Setup(x => x.ReplaceForRunbookAsync(
                runbook.Id,
                It.IsAny<IReadOnlyCollection<RunbookChunk>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repository
            .Setup(x => x.DeleteAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteRunbookHandler(
            repository.Object,
            chunkStore.Object);

        await handler.HandleAsync(runbook.Id);

        chunkStore.Verify(
            x => x.ReplaceForRunbookAsync(
                runbook.Id,
                It.Is<IReadOnlyCollection<RunbookChunk>>(chunks => chunks.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.DeleteAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRunbookDoesNotExist_ShouldThrow()
    {
        var id = Guid.NewGuid();

        var repository = new Mock<IRunbookRepository>();
        var chunkStore = new Mock<IRunbookChunkStore>();

        repository
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Runbook?)null);

        var handler = new DeleteRunbookHandler(
            repository.Object,
            chunkStore.Object);

        await Assert.ThrowsAsync<RunbookNotFoundException>(
            () => handler.HandleAsync(id));

        chunkStore.Verify(
            x => x.ReplaceForRunbookAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<RunbookChunk>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            x => x.DeleteAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenChunkCleanupFails_ShouldNotDeleteRunbook()
    {
        var runbook = Runbook.Create(
            "Title",
            "Description",
            "Service",
            "Content");

        var repository = new Mock<IRunbookRepository>();
        var chunkStore = new Mock<IRunbookChunkStore>();

        repository
            .Setup(x => x.GetByIdAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        chunkStore
            .Setup(x => x.ReplaceForRunbookAsync(
                runbook.Id,
                It.IsAny<IReadOnlyCollection<RunbookChunk>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Chunk cleanup failed."));

        var handler = new DeleteRunbookHandler(
            repository.Object,
            chunkStore.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(runbook.Id));

        repository.Verify(
            x => x.DeleteAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}