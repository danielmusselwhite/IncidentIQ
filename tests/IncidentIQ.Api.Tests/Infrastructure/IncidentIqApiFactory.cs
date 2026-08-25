using IncidentIQ.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentIQ.Api.Tests.Infrastructure;

public sealed class IncidentIqApiFactory : WebApplicationFactory<Program>
{
    public InMemoryIncidentRepository IncidentRepository { get; } = new();

    public InMemoryRunbookRepository RunbookRepository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs only initializes Cosmos in Development.
        // Testing replaces the real Cosmos repositories with in-memory implementations.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIncidentRepository>();
            services.AddSingleton<IIncidentRepository>(IncidentRepository);

            services.RemoveAll<IRunbookRepository>();
            services.AddSingleton<IRunbookRepository>(RunbookRepository);
        });
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}