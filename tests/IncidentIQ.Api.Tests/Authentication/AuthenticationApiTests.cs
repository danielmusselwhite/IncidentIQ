using IncidentIQ.Api.Authorization;
using IncidentIQ.Api.Contracts.Authentication;
using IncidentIQ.Api.Tests.Infrastructure;
using System.Net;

namespace IncidentIQ.Api.Tests.Authentication;

public sealed class AuthenticationApiTests(
    IncidentIqApiFactory factory)
    : IClassFixture<IncidentIqApiFactory>
{
    [Fact]
    public async Task ProtectedEndpoint_WhenAnonymous_ReturnsUnauthorized()
    {
        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/incidents");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenEngineerWithRequiredScope_ReturnsOk()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                IncidentIqRoles.Engineer);

        var response =
            await client.GetAsync(
                "/api/incidents");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenAdministratorWithRequiredScope_ReturnsOk()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                IncidentIqRoles.Administrator);

        var response =
            await client.GetAsync(
                "/api/incidents");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenAuthenticatedWithoutRole_ReturnsForbidden()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                role: null);

        var response =
            await client.GetAsync(
                "/api/incidents");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenRequiredScopeIsMissing_ReturnsForbidden()
    {
        using var client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.AuthenticatedHeaderName,
            "true");

        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.ScopeHeaderName,
            "some_other_scope");

        // Include a valid role so this test specifically proves
        // that the missing access_as_user scope causes the 403.
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.RoleHeaderName,
            IncidentIqRoles.Engineer);

        var response =
            await client.GetAsync(
                "/api/incidents");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WhenAnonymous_RemainsAccessible()
    {
        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task CurrentUser_WhenAdministrator_ReturnsAdministratorRole()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                IncidentIqRoles.Administrator);

        var response =
            await client.GetAsync("/api/me");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await response.Content
                .ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(user);

        Assert.Equal(
            "IncidentIQ Test User",
            user.Name);

        Assert.Contains(
            IncidentIqRoles.Administrator,
            user.Roles);
    }
}