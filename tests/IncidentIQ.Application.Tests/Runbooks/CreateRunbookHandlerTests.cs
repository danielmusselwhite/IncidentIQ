using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.Create;
using IncidentIQ.Domain.Runbooks;
using Moq;

namespace IncidentIQ.Application.Tests.Runbooks;

public sealed class CreateRunbookHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateRunbook()
    {
        var repository = new Mock<IRunbookRepository>();

        var handler = new CreateRunbookHandler(
            repository.Object,
            new CreateRunbookValidator());

        var command = new CreateRunbookCommand(
            "API Timeout Recovery",
            "Timeout troubleshooting steps.",
            "Orders API",
            "Check telemetry.");

        var result = await handler.HandleAsync(command);

        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(command.Service, result.Service);
        Assert.Equal(command.Content, result.Content);

        repository.Verify(
            x => x.CreateAsync(
                It.Is<Runbook>(r => r.Id == result.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCommand_ShouldThrowValidationException()
    {
        var repository = new Mock<IRunbookRepository>();

        var handler = new CreateRunbookHandler(
            repository.Object,
            new CreateRunbookValidator());

        var command = new CreateRunbookCommand(
            "",
            "Description",
            "Service",
            "Content");

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(command));

        repository.Verify(
            x => x.CreateAsync(
                It.IsAny<Runbook>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}