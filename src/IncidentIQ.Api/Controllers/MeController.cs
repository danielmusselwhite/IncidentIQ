using IncidentIQ.Api.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IncidentIQ.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    /// <summary>
    /// Gets the current authenticated user.
    /// Deconstructing their JWT token to get their username and roles.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> Get()
    {
        var roles = User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct()
            .Order()
            .ToArray();

        var username =
            User.FindFirst("preferred_username")?.Value ??
            User.FindFirst(ClaimTypes.Upn)?.Value ??
            User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(
            new CurrentUserResponse(
                User.Identity?.Name,
                username,
                roles));
    }
}