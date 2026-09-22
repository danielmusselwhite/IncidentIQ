using IncidentIQ.Api.Authorization;
using IncidentIQ.Api.Tests.Fakes;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentIQ.Api.Tests.Infrastructure;

public sealed class IncidentIqApiFactory
    : WebApplicationFactory<Program>
{
    public InMemoryIncidentRepository IncidentRepository { get; } =
        new();

    public InMemoryRunbookRepository RunbookRepository { get; } =
        new();

    public InMemoryIncidentAnalysisQueue AnalysisQueue { get; } =
        new();

    public InMemoryIncidentSubmissionStore IncidentSubmissionStore
    {
        get;
    }

    public InMemoryIncidentAnalysisReader IncidentAnalysisReader { get; } =
        new();

    public InMemoryRunbookChunkStore RunbookChunkStore { get; } =
        new();

    public InMemoryRunbookChunkRetriever RunbookChunkRetriever { get; } =
        new();

    public IncidentIqApiFactory()
    {
        IncidentSubmissionStore =
            new InMemoryIncidentSubmissionStore(
                IncidentRepository);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        // Program.cs only initializes Cosmos in Development.
        // Testing replaces real infrastructure with in-memory implementations.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIncidentRepository>();
            services.AddSingleton<IIncidentRepository>(
                IncidentRepository);

            services.RemoveAll<IRunbookRepository>();
            services.AddSingleton<IRunbookRepository>(
                RunbookRepository);

            services.RemoveAll<IIncidentAnalysisQueue>();
            services.AddSingleton<IIncidentAnalysisQueue>(
                AnalysisQueue);

            services.RemoveAll<IIncidentSubmissionStore>();
            services.AddSingleton<IIncidentSubmissionStore>(
                IncidentSubmissionStore);

            services.RemoveAll<IIncidentAnalysisReader>();
            services.AddSingleton<IIncidentAnalysisReader>(
                IncidentAnalysisReader);

            services.RemoveAll<IRunbookChunkStore>();
            services.AddSingleton<IRunbookChunkStore>(
                RunbookChunkStore);

            services.RemoveAll<IRunbookChunkRetriever>();
            services.AddSingleton<IRunbookChunkRetriever>(
                RunbookChunkRetriever);
        });

        // Replace production Entra bearer authentication with a deterministic
        // authentication scheme used only by the API integration-test host.
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.SchemeName;

                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ =>
                        {
                        });
        });
    }

    /// <summary>
    /// Creates an authenticated HTTP client.
    ///
    /// Engineer is the default role because most integration tests represent
    /// normal IncidentIQ application usage.
    /// Pass null to create an authenticated user with no application role.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        string? role = IncidentIqRoles.Engineer)
    {
        var client =
            CreateClient();

        AddTestAuthentication(
            client,
            role);

        return client;
    }

    /// <summary>
    /// Creates an authenticated HTTPS client for tests that depend on an HTTPS
    /// base address, such as CreatedAtAction/Location header assertions.
    /// </summary>
    public HttpClient CreateHttpsClient(
        string? role = IncidentIqRoles.Engineer)
    {
        var client =
            CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("https://localhost")
                });

        AddTestAuthentication(
            client,
            role);

        return client;
    }

    private static void AddTestAuthentication(
        HttpClient client,
        string? role)
    {
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.AuthenticatedHeaderName,
            "true");

        if (!string.IsNullOrWhiteSpace(role))
        {
            client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.RoleHeaderName,
                role);
        }
    }
}