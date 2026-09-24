using IncidentIQ.Api.Authorization;
using IncidentIQ.Api.Contracts.Operations;
using IncidentIQ.Api.Tests.Infrastructure;
using IncidentIQ.Domain.Incidents;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentIQ.Api.Tests.Operations;

public sealed class OperationsApiTests(
    IncidentIqApiFactory factory)
    : IClassFixture<IncidentIqApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions =
    new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    [Fact]
    public async Task GetFailedIncidents_WhenAdministrator_ReturnsOnlyFailedIncidents()
    {
        factory.IncidentRepository.Clear();

        var failedIncident = CreateFailedIncident();
        var activeIncident = CreateIncident();

        await factory.IncidentRepository.CreateAsync(
            failedIncident);

        await factory.IncidentRepository.CreateAsync(
            activeIncident);

        using var client =
            factory.CreateAuthenticatedClient(
                IncidentIqRoles.Administrator);

        var response =
            await client.GetAsync(
                "/api/operations/failed-incidents");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var incidents =
            await response.Content
                .ReadFromJsonAsync<
                    FailedIncidentResponse[]>(
                        JsonOptions);

        var incident =
            Assert.Single(incidents!);

        Assert.Equal(
            failedIncident.Id,
            incident.Id);

        Assert.Equal(
            "Analysis failed.",
            incident.FailureReason);
    }

    [Fact]
    public async Task GetFailedIncidents_WhenEngineer_ReturnsForbidden()
    {
        using var client =
            factory.CreateAuthenticatedClient(
                IncidentIqRoles.Engineer);

        var response =
            await client.GetAsync(
                "/api/operations/failed-incidents");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private static Incident CreateIncident()
    {
        return Incident.Create(
            "Payments API timeout",
            "Checkout requests are timing out.",
            "Payments",
            "Production",
            IncidentSeverity.High,
            "Database timeout errors");
    }

    private static Incident CreateFailedIncident()
    {
        var incident = CreateIncident();

        incident.StartProcessingAttempt();
        incident.MarkFailed(
            "Analysis failed.");

        return incident;
    }
}