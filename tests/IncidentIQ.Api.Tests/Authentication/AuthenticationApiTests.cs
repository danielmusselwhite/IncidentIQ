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
    public async Task ProtectedEndpoint_WhenAuthenticatedWithRequiredScope_ReturnsOk()
    {
        using var client =
            factory.CreateAuthenticatedClient();

        var response =
            await client.GetAsync(
                "/api/incidents");

        Assert.Equal(
            HttpStatusCode.OK,
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
}