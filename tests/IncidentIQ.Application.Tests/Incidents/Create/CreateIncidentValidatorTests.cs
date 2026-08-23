using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Tests.Incidents.Create;

public sealed class CreateIncidentValidatorTests
{
    private readonly CreateIncidentValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = CreateValidCommand();

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validate_WithEmptyTitle_IsInvalid(string title)
    {
        var command = CreateValidCommand() with { Title = title };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(command.Title));
    }

    [Fact]
    public async Task Validate_WithTitleOverMaximumLength_IsInvalid()
    {
        var command = CreateValidCommand() with
        {
            Title = new string('A', 201)
        };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(command.Title));
    }

    [Theory]
    [InlineData("Description")]
    [InlineData("Service")]
    [InlineData("Environment")]
    public async Task Validate_WithMissingRequiredField_IsInvalid(string propertyName)
    {
        var command = CreateValidCommand();

        command = propertyName switch
        {
            "Description" => command with { Description = "" },
            "Service" => command with { Service = "" },
            "Environment" => command with { Environment = "" },
            _ => command
        };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == propertyName);
    }

    [Fact]
    public async Task Validate_WithInvalidSeverity_IsInvalid()
    {
        var command = CreateValidCommand() with
        {
            Severity = (IncidentSeverity)999
        };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(command.Severity));
    }

    private static CreateIncidentCommand CreateValidCommand()
    {
        return new CreateIncidentCommand(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");
    }
}