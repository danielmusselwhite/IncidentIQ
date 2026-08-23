using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Create;

/// <summary>
/// Represents a handler for the <see cref="CreateIncidentCommand"/>.
/// (What the application does with the request.)
/// </summary>
/// <param name="incidentRepository"></param>
/// <param name="validator"></param>
public sealed class CreateIncidentHandler(
    IIncidentRepository incidentRepository,
    IValidator<CreateIncidentCommand> validator)
{
    /// <summary>
    /// Handles the creation of an incident based on the provided <see cref="CreateIncidentCommand"/>.
    /// Validates the command using FluentValidation and, if valid, creates a new incident in the repository.
    /// </summary>
    /// <param name="command">The command containing the details of the incident to be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created incident.</returns>
    public async Task<Incident> HandleAsync(
        CreateIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var incident = Incident.Create(
            command.Title,
            command.Description,
            command.Service,
            command.Environment,
            command.Severity,
            command.Symptoms);

        return await incidentRepository.CreateAsync(incident, cancellationToken);
    }
}