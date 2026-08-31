using IncidentIQ.Api.Contracts.Incidents;
using IncidentIQ.Application.Analyse;
using IncidentIQ.Application.Analyse.Retry;
using IncidentIQ.Application.Incidents.Create;
using IncidentIQ.Application.Incidents.GetAll;
using IncidentIQ.Application.Incidents.GetById;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace IncidentIQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IncidentsController(CreateIncidentHandler createIncidentHandler,
    GetAllIncidentsHandler getAllIncidentsHandler,
    GetIncidentByIdHandler getIncidentByIdHandler,
    RetryAnalyseIncidentHandler retryIncidentAnalysisHandler) : ControllerBase
{
    /// <summary>
    /// Creates a new incident.
    /// </summary>
    /// <param name="request">The request containing the details of the incident to create.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created incident.</returns>
    [HttpPost]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentResponse>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        // convert to application command
        var command = new CreateIncidentCommand(
            request.Title,
            request.Description,
            request.Service,
            request.Environment,
            request.Severity,
            request.Symptoms);

        // generate correlationId for logging and tracing
        var correlationId = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;

        //!IMPORTANT add correlationId to response headers for client-side tracing
        Response.Headers["X-Correlation-ID"] = correlationId;

        // call the application handler to create the incident, getting back the domain incident
        var incident = await createIncidentHandler.HandleAsync(command, correlationId, cancellationToken);
        var response = IncidentResponse.FromDomain(incident); // convert to response DTO

        // return a 201 Created response with the location of the new incident
        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, response);
    }


    /// <summary>
    /// Retrieves an incident by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the incident.</param>
    /// <returns>The incident with the specified unique identifier.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> GetById(
    string id,
    CancellationToken cancellationToken)
    {
        var incident = await getIncidentByIdHandler.HandleAsync(id, cancellationToken);

        return Ok(IncidentResponse.FromDomain(incident));
    }


    /// <summary>
    /// Retrieves all incidents.s
    /// </summary>
    /// <returns>The incident with the specified unique identifier.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<IncidentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<IncidentResponse>>> GetAll(    CancellationToken cancellationToken)
    {
        var incidents = await getAllIncidentsHandler.HandleAsync(cancellationToken);

        // convert all domain incidents to response DTOs
        var response = incidents
            .Select(IncidentResponse.FromDomain)
            .ToArray();

        return Ok(response);
    }


    [HttpPost("{id}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidentResponse>> Retry(string id, CancellationToken cancellationToken)
    {
        // generate correlationId for logging and tracing
        var correlationId = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;

        //!IMPORTANT add correlationId to response headers for client-side tracing
        Response.Headers["X-Correlation-ID"] = correlationId;

        // create the retry command with the incident id and correlation id
        var retryCommand = new RetryAnalyseIncidentCommand(id, correlationId);

        // retry the incient
        var retriedIncident = await retryIncidentAnalysisHandler.HandleAsync(retryCommand, cancellationToken);

        // return the retried incident as the response body
        return AcceptedAtAction(
            nameof(GetById),
            new { id = retriedIncident.Id },
            IncidentResponse.FromDomain(retriedIncident));
    }
}