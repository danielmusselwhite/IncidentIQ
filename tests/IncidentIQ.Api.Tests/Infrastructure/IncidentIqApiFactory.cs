using IncidentIQ.Api.Tests.Fakes;
using IncidentIQ.Application.Common.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentIQ.Api.Tests.Infrastructure;

public sealed class IncidentIqApiFactory : WebApplicationFactory<Program>
{
    public InMemoryIncidentRepository IncidentRepository { get; } = new();

    public InMemoryRunbookRepository RunbookRepository { get; } = new();

    public InMemoryIncidentAnalysisQueue AnalysisQueue { get; } = new();

    public InMemoryIncidentSubmissionStore IncidentSubmissionStore { get; }

    public InMemoryIncidentAnalysisReader IncidentAnalysisReader { get; } = new();

    public IncidentIqApiFactory()
    {
        IncidentSubmissionStore = new InMemoryIncidentSubmissionStore(IncidentRepository);
    }

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

            services.RemoveAll<IIncidentAnalysisQueue>();
            services.AddSingleton<IIncidentAnalysisQueue>(AnalysisQueue);

            services.RemoveAll<IIncidentSubmissionStore>();
            services.AddSingleton<IIncidentSubmissionStore>(IncidentSubmissionStore);

            services.RemoveAll<IIncidentAnalysisReader>();
            services.AddSingleton<IIncidentAnalysisReader>(IncidentAnalysisReader);
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