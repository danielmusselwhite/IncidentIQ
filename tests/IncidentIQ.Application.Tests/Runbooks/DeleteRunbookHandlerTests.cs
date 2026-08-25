using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Runbooks.Delete;
using IncidentIQ.Domain.Runbooks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks;

public sealed class DeleteRunbookHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRunbookExists_ShouldDeleteRunbook()
    {
        var runbook = Runbook.Create(
            "Title",
            "Description",
            "Service",
            "Content");

        var repository = new Mock<IRunbookRepository>();

        repository
            .Setup(x => x.GetByIdAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        var handler = new DeleteRunbookHandler(repository.Object);

        await handler.HandleAsync(runbook.Id);

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

        repository
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Runbook?)null);

        var handler = new DeleteRunbookHandler(repository.Object);

        await Assert.ThrowsAsync<RunbookNotFoundException>(
            () => handler.HandleAsync(id));
    }
}