using FluentValidation.TestHelper;
using IncidentIQ.Application.Runbooks.Create;

namespace IncidentIQ.Application.Tests.Runbooks;

public sealed class CreateRunbookValidatorTests
{
    private readonly CreateRunbookValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new CreateRunbookCommand(
            "API Timeout Recovery",
            "How to investigate timeout incidents.",
            "Orders API",
            "Check Application Insights and downstream dependencies.");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldFail()
    {
        var command = new CreateRunbookCommand(
            "",
            "Description",
            "Orders API",
            "Content");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithEmptyDescription_ShouldFail()
    {
        var command = new CreateRunbookCommand(
            "Title",
            "",
            "Orders API",
            "Content");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithEmptyService_ShouldFail()
    {
        var command = new CreateRunbookCommand(
            "Title",
            "Description",
            "",
            "Content");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Service);
    }

    [Fact]
    public void Validate_WithEmptyContent_ShouldFail()
    {
        var command = new CreateRunbookCommand(
            "Title",
            "Description",
            "Orders API",
            "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
}