using IncidentIQ.Application.Incidents.Analyse;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Application.Incidents.HistoricalSearch.Retrieve;
using IncidentIQ.Application.Runbooks.RetrieveChunks;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Tests.Incidents.Analyse.Grounding;

public sealed class EvidenceReferenceValidatorTests
{
    [Fact]
    public void Validate_WhenReferencesExistInContext_DoesNotThrow()
    {
        // Arrange
        var context = CreateContext(
            historicalIncidentCount: 2,
            runbookChunkCount: 2);

        var references = new[]
        {
            "HI-1",
            "HI-2",
            "RB-1",
            "RB-2"
        };

        // Act
        var exception = Record.Exception(
            () => EvidenceReferenceValidator.Validate(references, context));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenHistoricalIncidentReferenceWasNotSupplied_ThrowsException()
    {
        // Arrange
        var context = CreateContext(
            historicalIncidentCount: 1,
            runbookChunkCount: 0);

        var references = new[]
        {
            "HI-2"
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => EvidenceReferenceValidator.Validate(references, context));

        // Assert
        Assert.Contains("HI-2", exception.Message);
    }

    [Fact]
    public void Validate_WhenRunbookReferenceWasNotSupplied_ThrowsException()
    {
        // Arrange
        var context = CreateContext(
            historicalIncidentCount: 0,
            runbookChunkCount: 1);

        var references = new[]
        {
            "RB-2"
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => EvidenceReferenceValidator.Validate(references, context));

        // Assert
        Assert.Contains("RB-2", exception.Message);
    }

    [Fact]
    public void Validate_WhenNoEvidenceWasRetrievedAndNoReferencesReturned_DoesNotThrow()
    {
        // Arrange
        var context = CreateContext(
            historicalIncidentCount: 0,
            runbookChunkCount: 0);

        // Act
        var exception = Record.Exception(
            () => EvidenceReferenceValidator.Validate([], context));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenNoEvidenceWasRetrievedButReferenceReturned_ThrowsException()
    {
        // Arrange
        var context = CreateContext(
            historicalIncidentCount: 0,
            runbookChunkCount: 0);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => EvidenceReferenceValidator.Validate(["HI-1"], context));

        // Assert
        Assert.Contains("HI-1", exception.Message);
    }

    [Fact]
    public void Validate_WhenMultipleInvalidReferencesReturned_ReportsEachDistinctReference()
    {
        // Arrange
        var context = CreateContext(
            historicalIncidentCount: 1,
            runbookChunkCount: 1);

        var references = new[]
        {
            "HI-99",
            "RB-99",
            "HI-99"
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => EvidenceReferenceValidator.Validate(references, context));

        // Assert
        Assert.Contains("HI-99", exception.Message);
        Assert.Contains("RB-99", exception.Message);
    }

    private static IncidentAnalysisContext CreateContext(
        int historicalIncidentCount,
        int runbookChunkCount)
    {
        var incident = new IncidentAnalysisInput(
            Title: "Payment gateway failure",
            Description: "Customers cannot complete payments.",
            Service: "Payments",
            Environment: "Production",
            Severity: IncidentSeverity.High,
            Symptoms: "HTTP 503 responses");

        var historicalIncidents = Enumerable.Range(1, historicalIncidentCount)
            .Select(index => new HistoricalIncidentMatch(
                IncidentId: Guid.NewGuid(),
                Title: $"Historical Incident {index}",
                Description: "Previous payment failure.",
                Symptoms: "HTTP 503 responses",
                Service: "Payments",
                Environment: "Production",
                Severity: IncidentSeverity.High,
                CompletedAtUtc: DateTimeOffset.UtcNow.AddDays(-index),
                Distance: 0.8))
            .ToList();

        var runbookChunks = Enumerable.Range(1, runbookChunkCount)
            .Select(index => new RunbookChunkMatch(
                RunbookId: Guid.NewGuid(),
                ChunkIndex: index,
                Title: "Payment Gateway Recovery",
                Service: "Payments",
                Content: "Check the external payment provider.",
                Distance: 0.8))
            .ToList();

        return new IncidentAnalysisContext(
            Incident: incident,
            HistoricalIncidents: historicalIncidents,
            RunbookChunks: runbookChunks);
    }
}