using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Runbooks.GetById;
using IncidentIQ.Domain.Runbooks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks;

public sealed class GetRunbookByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRunbookExists_ShouldReturnRunbook()
    {
        var runbook = Runbook.Create(
            "Runbook",
            "Description",
            "Orders API",
            "Content");

        var repository = new Mock<IRunbookRepository>();

        repository
            .Setup(x => x.GetByIdAsync(
                runbook.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        var handler = new GetRunbookByIdHandler(repository.Object);

        var result = await handler.HandleAsync(runbook.Id);

        Assert.Equal(runbook.Id, result.Id);
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

        var handler = new GetRunbookByIdHandler(repository.Object);

        await Assert.ThrowsAsync<RunbookNotFoundException>(
            () => handler.HandleAsync(id));
    }
}