using FluentValidation;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Analyse;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Create;

/// <summary>
/// Handles the creation of incidents and queues them for asynchronous analysis.
/// </summary>
/// <param name="incidentRepository">
/// Repository used to persist the incident.
/// </param>
/// <param name="incidentAnalysisQueue">
/// Queue used to request asynchronous incident analysis.
/// </param>
/// <param name="validator">
/// Validator used to validate the create incident command.
/// </param>
public sealed class CreateIncidentHandler(
    IIncidentSubmissionStore incidentSubmissionStore,
    IValidator<CreateIncidentCommand> validator)
{
    /// <summary>
    /// Validates and creates an incident, then queues it for asynchronous analysis.
    /// </summary>
    /// <param name="command">
    /// The command containing the details of the incident to create.
    /// </param>
    /// <param name="correlationId">
    /// Identifier used to correlate the API request with the asynchronous analysis workflow.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>The created incident.</returns>
    public async Task<Incident> HandleAsync(
        CreateIncidentCommand command,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // Validate the incoming command before creating or persisting anything.
        await validator.ValidateAndThrowAsync(
            command,
            cancellationToken);

        // Create the domain entity.
        var incident = Incident.Create(
            command.Title,
            command.Description,
            command.Service,
            command.Environment,
            command.Severity,
            command.Symptoms);

        // create the analyse incident command
        var analyseIncidentCommand = new AnalyseIncidentCommand(
            CommandId: Guid.NewGuid(),
            IncidentId: incident.Id,
            CorrelationId: correlationId,
            QueuedAtUtc: incident.CreatedAt);

        // create the incident + outbox message in the submission store
        // (this outbox will be relayed to the se)
        return await incidentSubmissionStore.CreateAsync(
            incident,
            analyseIncidentCommand,
            cancellationToken);
    }
}