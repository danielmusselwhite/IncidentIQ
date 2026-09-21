using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IncidentIQ.Api.Tests.Infrastructure;

/// <summary>
/// Provides a deterministic authenticated identity for API integration tests.
///
/// Production uses Microsoft Entra JWT bearer authentication. Tests replace the
/// default authentication scheme so they do not depend on real Entra tokens.
/// </summary>
internal sealed class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName =
        "IncidentIQ-Test";

    public const string AuthenticatedHeaderName =
        "X-Test-Authenticated";

    public const string ScopeHeaderName =
        "X-Test-Scope";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(
            options,
            logger,
            encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        // No test-authentication header represents an anonymous request.
        if (!Request.Headers.TryGetValue(
                AuthenticatedHeaderName,
                out var authenticated) ||
            !string.Equals(
                authenticated.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    "test-user-id"),

                new(
                    ClaimTypes.Name,
                    "IncidentIQ Test User")
            };

        // Normal authenticated test clients receive the same delegated scope
        // that the React SPA will request from Microsoft Entra.
        var scope =
            Request.Headers.TryGetValue(
                ScopeHeaderName,
                out var requestedScope)
                ? requestedScope.ToString()
                : "access_as_user";

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(
                new Claim(
                    "scp",
                    scope));
        }

        var identity =
            new ClaimsIdentity(
                claims,
                SchemeName);

        var principal =
            new ClaimsPrincipal(
                identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(
                ticket));
    }
}