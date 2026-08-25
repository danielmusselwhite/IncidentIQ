using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Runbooks.Update;
using IncidentIQ.Domain.Runbooks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks;

public sealed class UpdateRunbookHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRunbookExists_ShouldUpdateRunbook()
    {
        var runbook = Runbook.Create(
            "Old Title",
            "Old Description",
            "Old Service",
            "Old Content");

        var repository = new Mock<IRunbookRepository>();

        repository
            .Setup(x => x.GetByIdAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        var handler = new UpdateRunbookHandler(
            repository.Object,
            new UpdateRunbookValidator());

        var command = new UpdateRunbookCommand(
            runbook.Id,
            "New Title",
            "New Description",
            "New Service",
            "New Content");

        var result = await handler.HandleAsync(command);

        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Description", result.Description);
        Assert.Equal("New Service", result.Service);
        Assert.Equal("New Content", result.Content);

        repository.Verify(
            x => x.UpdateAsync(
                runbook,
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

        var handler = new UpdateRunbookHandler(
            repository.Object,
            new UpdateRunbookValidator());

        var command = new UpdateRunbookCommand(
            id,
            "Title",
            "Description",
            "Service",
            "Content");

        await Assert.ThrowsAsync<RunbookNotFoundException>(
            () => handler.HandleAsync(command));
    }
}