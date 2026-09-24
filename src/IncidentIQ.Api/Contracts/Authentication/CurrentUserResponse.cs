namespace IncidentIQ.Api.Contracts.Authentication;

public sealed record CurrentUserResponse(
    string? Name,
    string? Username,
    IReadOnlyCollection<string> Roles);