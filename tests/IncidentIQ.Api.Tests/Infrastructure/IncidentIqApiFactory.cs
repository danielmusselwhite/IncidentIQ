using IncidentIQ.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentIQ.Api.Tests.Infrastructure;

public sealed class IncidentIqApiFactory : WebApplicationFactory<Program>
{
    public InMemoryIncidentRepository Repository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // as program.cs is configured to use Cosmos emulator in Debug vs live Cosmos in prod, it won't use Cosmos emulator in Testing environment, so we can use in-memory repo for testing safely

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIncidentRepository>();
            services.AddSingleton<IIncidentRepository>(Repository);
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