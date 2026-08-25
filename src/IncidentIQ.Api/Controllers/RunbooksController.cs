using IncidentIQ.Api.Contracts.Runbooks;
using IncidentIQ.Application.Runbooks.Create;
using IncidentIQ.Application.Runbooks.Delete;
using IncidentIQ.Application.Runbooks.GetAll;
using IncidentIQ.Application.Runbooks.GetById;
using IncidentIQ.Application.Runbooks.Update;
using Microsoft.AspNetCore.Mvc;

namespace IncidentIQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RunbooksController(
    CreateRunbookHandler createRunbookHandler,
    GetRunbookByIdHandler getRunbookByIdHandler,
    GetAllRunbooksHandler getAllRunbooksHandler,
    UpdateRunbookHandler updateRunbookHandler,
    DeleteRunbookHandler deleteRunbookHandler)
    : ControllerBase
{

    /// <summary>
    /// Creates a new runbook.
    /// </summary>
    /// <param name="request">The request DTO containing the runbook details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created runbook response DTO.</returns>
    [HttpPost]
    [ProducesResponseType<RunbookResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RunbookResponse>> Create(
        CreateRunbookRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRunbookCommand(
            request.Title,
            request.Description,
            request.Service,
            request.Content);

        var runbook = await createRunbookHandler.HandleAsync(
            command,
            cancellationToken);

        var response = RunbookResponse.FromDomain(runbook);

        return CreatedAtAction(
            nameof(GetById),
            new { id = runbook.Id },
            response);
    }

    /// <summary>
    /// Retrieves all runbooks.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of runbook response DTOs.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<RunbookResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RunbookResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var runbooks = await getAllRunbooksHandler.HandleAsync(
            cancellationToken);

        var response = runbooks
            .Select(RunbookResponse.FromDomain)
            .ToArray();

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a runbook by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The runbook response DTO.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RunbookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunbookResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var runbook = await getRunbookByIdHandler.HandleAsync(
            id,
            cancellationToken);

        return Ok(RunbookResponse.FromDomain(runbook));
    }

    /// <summary>
    /// Updates an existing runbook by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook.</param>
    /// <param name="request">The request DTO containing the updated runbook details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated runbook response DTO.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RunbookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunbookResponse>> Update(
        Guid id,
        UpdateRunbookRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRunbookCommand(
            id,
            request.Title,
            request.Description,
            request.Service,
            request.Content);

        var runbook = await updateRunbookHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(RunbookResponse.FromDomain(runbook));
    }

    /// <summary>
    /// Deletes a runbook by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the runbook.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await deleteRunbookHandler.HandleAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}