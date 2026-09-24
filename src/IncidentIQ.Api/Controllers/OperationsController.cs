using IncidentIQ.Api.Authorization;
using IncidentIQ.Api.Contracts.Operations;
using IncidentIQ.Application.Incidents.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentIQ.Api.Controllers;

[ApiController]
[Route("api/operations")]
[Authorize(Policy = IncidentIqPolicies.AdministratorAccess)]
public sealed class OperationsController(
    GetFailedIncidentsHandler getFailedIncidentsHandler)
    : ControllerBase
{

    [HttpGet("failed-incidents")]
    [ProducesResponseType<IReadOnlyCollection<FailedIncidentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<FailedIncidentResponse>>> GetFailedIncidents(CancellationToken cancellationToken)
    {
        var incidents =
            await getFailedIncidentsHandler.HandleAsync(
                cancellationToken);

        var response =
            incidents
                .Select(
                    FailedIncidentResponse.FromDomain)
                .ToArray();

        return Ok(response);
    }
}