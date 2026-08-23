using FluentValidation;

namespace IncidentIQ.Application.Incidents.Create;

/// <summary>
/// Represents a validator for the <see cref="CreateIncidentCommand"/>.
/// Utilizes FluentValidation to ensure that the command's properties meet the required criteria before processing.
/// </summary>
public sealed class CreateIncidentValidator : AbstractValidator<CreateIncidentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateIncidentValidator"/> class.
    /// Validates that the properties of the <see cref="CreateIncidentCommand"/> are not empty, have appropriate lengths, and that the severity is a valid enum value.
    /// Utilizes FluentValidation's built-in methods to enforce these rules, ensuring that the command is valid before it is processed by the application.
    /// </summary>
    public CreateIncidentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(5000)
            .WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Service)
            .NotEmpty()
            .WithMessage("Service is required.");

        RuleFor(x => x.Environment)
            .NotEmpty()
            .WithMessage("Environment is required.");

        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Severity is invalid.");
    }
}