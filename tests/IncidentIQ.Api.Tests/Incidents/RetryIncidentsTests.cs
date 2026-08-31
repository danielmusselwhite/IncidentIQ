using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Retry;
using IncidentIQ.Domain.Incidents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Timers;

namespace IncidentIQ.Api.Tests.Incidents;

public sealed class RetryIncidentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RetryIncidentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Retry_WhenIncidentIsFailed_ShouldReturnAccepted()
    {
        // Arrange
        var incident = CreateFailedIncident();

        var (client, _, submissionStore) = CreateClient(incident);

        // Act
        var response = await client.PostAsync(
            $"/api/incidents/{incident.Id}/retry",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(
            $"/api/incidents/{incident.Id}",
            response.Headers.Location.ToString());

        submissionStore.Verify(
            store => store.RetryAsync(
                It.Is<Incident>(retriedIncident =>
                    retriedIncident.Id == incident.Id &&
                    retriedIncident.Status == IncidentStatus.Queued),
                It.Is<AnalyseIncidentCommand>(command =>
                    command.IncidentId == incident.Id &&
                    command.CommandId != Guid.Empty &&
                    !string.IsNullOrWhiteSpace(command.CorrelationId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Retry_WhenIncidentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var incidentId = Guid.NewGuid().ToString();

        var (client, _, submissionStore) = CreateClient(null);

        // Act
        var response = await client.PostAsync(
            $"/api/incidents/{incidentId}/retry",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Incident not found", problemDetails.Title);

        submissionStore.Verify(
            store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Retry_WhenIncidentIsNotFailed_ShouldReturnConflict()
    {
        // Arrange
        var incident = CreateIncident();

        var (client, _, submissionStore) = CreateClient(incident);

        // Act
        var response = await client.PostAsync(
            $"/api/incidents/{incident.Id}/retry",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("Incident not retryable", problemDetails.Title);

        submissionStore.Verify(
            store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private (
        HttpClient Client,
        Mock<IIncidentRepository> IncidentRepository,
        Mock<IIncidentSubmissionStore> SubmissionStore)
        CreateClient(Incident? incident)
    {
        var incidentRepository = new Mock<IIncidentRepository>();
        var submissionStore = new Mock<IIncidentSubmissionStore>();

        incidentRepository
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        submissionStore
            .Setup(store => store.RetryAsync(
                It.IsAny<Incident>(),
                It.IsAny<AnalyseIncidentCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns((Incident retriedIncident, AnalyseIncidentCommand _, CancellationToken _) =>
                Task.FromResult(retriedIncident));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIncidentRepository>();
                services.RemoveAll<IIncidentSubmissionStore>();

                services.AddSingleton(incidentRepository.Object);
                services.AddSingleton(submissionStore.Object);

                // Ensure the real handler is used with our mocked dependencies.
                services.RemoveAll<RetryAnalyseIncidentHandler>();
                services.AddScoped<RetryAnalyseIncidentHandler>();
            });
        });

        return (
            factory.CreateClient(),
            incidentRepository,
            submissionStore);
    }

    private static Incident CreateIncident()
    {
        return Incident.Create(
            "Payment API unavailable",
            "Payment API is returning errors.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "HTTP 500 responses");
    }

    private static Incident CreateFailedIncident()
    {
        var incident = CreateIncident();

        incident.StartProcessingAttempt();
        incident.MarkFailed("Analysis failed.");

        return incident;
    }
}