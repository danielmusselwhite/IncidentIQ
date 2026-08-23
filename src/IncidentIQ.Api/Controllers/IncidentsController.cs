using IncidentIQ.Api.Contracts.Incidents;
using IncidentIQ.Application.Incidents.Create;
using Microsoft.AspNetCore.Mvc;

namespace IncidentIQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IncidentsController(CreateIncidentHandler createIncidentHandler) : ControllerBase
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

        // call the application handler to create the incident, getting back the domain incident
        var incident = await createIncidentHandler.HandleAsync(command, cancellationToken);
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
    public IActionResult GetById(string id)
    {
        throw new NotImplementedException(); // todo - implement me
    }
}