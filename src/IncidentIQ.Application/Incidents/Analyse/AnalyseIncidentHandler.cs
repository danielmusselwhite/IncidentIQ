using System.Diagnostics;
using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Application.Common.Telemetry;
using IncidentIQ.Application.Incidents.Analyse.Grounding;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Incidents.Analyse;

public sealed class AnalyseIncidentHandler(IIncidentRepository incidentRepository, IIncidentAnalyzer incidentAnalyzer, IIncidentAnalysisStore incidentAnalysisStore, IncidentAnalysisContextBuilder incidentAnalysisContextBuilder)
{
    /// <summary>
    /// Handles the analysis of an incident based on the provided command. It retrieves the incident from the repository, checks its status, and performs the analysis if it hasn't been completed yet. If the incident is already completed, it treats the operation as a no-op to ensure idempotency.
    /// </summary>
    /// <param name="command">The command containing the incident ID to analyze.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="IncidentNotFoundException">Thrown if the incident with the specified ID is not found.</exception>
    public async Task HandleAsync(
    AnalyseIncidentCommand command,
    CancellationToken cancellationToken = default)
    {
        #region Open Telemetry Tracking
        using var activity =
            IncidentIqTelemetry.ActivitySource.StartActivity(
                "incident.analysis",
                ActivityKind.Internal);

        activity?.SetTag("incident.id", command.IncidentId);
        activity?.SetTag("incident.command_id", command.CommandId);
        activity?.SetTag("incident.correlation_id", command.CorrelationId);
        activity?.SetTag("incident.queued_at", command.QueuedAtUtc);
        #endregion

        // first, get the incident we have been messaged to analyse
        var incident = await incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);
        if (incident is null) throw new IncidentNotFoundException(command.IncidentId);

        // basic state-based idempotency: (if incident has already been processed, return and treat as a no-op so worker can safely complete it, prevents same work from being done multiple times)
        if (incident.Status == IncidentStatus.Completed) return;

        // Persist Processing before the potentially long-running AI call.
        incident.StartProcessingAttempt();
        await incidentRepository.UpdateAsync(incident, cancellationToken);

        // prepare the input for the AI analysis
        var analysisInput = new IncidentAnalysisInput(
                Title: incident.Title,
                Description: incident.Description,
                Service: incident.Service,
                Environment: incident.Environment,
                Severity: incident.Severity,
                Symptoms: incident.Symptoms);

        IncidentAnalysisContext analysisContext;

        using (var retrievalActivity = IncidentIqTelemetry.ActivitySource.StartActivity("incident.analysis.retrieve_context", ActivityKind.Internal))
        {
            retrievalActivity?.SetTag("incident.service", incident.Service);
            retrievalActivity?.SetTag("incident.environment", incident.Environment);

            analysisContext = await incidentAnalysisContextBuilder.BuildAsync(analysisInput, cancellationToken); // Retrieve the historical Incident and Runbook evidence that will ground the AI analysis.


            retrievalActivity?.SetTag("incident.historical_matches", analysisContext.HistoricalIncidents.Count);
            retrievalActivity?.SetTag("incident.runbook_matches", analysisContext.RunbookChunks.Count);
        }

        IncidentAnalysisResult analysisResult;

        using (var aiActivity = IncidentIqTelemetry.ActivitySource.StartActivity("incident.analysis.generate", ActivityKind.Client))
        {
            analysisResult = await incidentAnalyzer.AnalyzeIncidentAsync(
                    analysisContext,
                    cancellationToken);

            aiActivity?.SetTag("gen_ai.response.model", analysisResult.Model);
        }

        // Build the durable snapshot from the exact evidence supplied to this analysis. This ensures that the evidence is persisted in the same state as it was when the analysis was performed, even if the underlying data changes later.
        var analysisEvidence = IncidentAnalysisEvidenceBuilder.Build(analysisContext);

        // Do not persist Completed separately: the store atomically commits the completed Incident and its IncidentAnalysis document.
        incident.MarkCompleted();

        using (var persistenceActivity = IncidentIqTelemetry.ActivitySource.StartActivity("incident.analysis.persist", ActivityKind.Internal))
        {
            await incidentAnalysisStore.StoreCompletedAnalysisAsync(
                    incident,
                    analysisResult,
                    analysisEvidence,
                    cancellationToken);
        }
    }

    /// <summary>
    /// Marks the specified incident as failed with the given failure reason.
    /// </summary>
    public async Task MarkFailedAsync(
        AnalyseIncidentCommand command,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var incident = await incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
            return;

        if (incident.Status is IncidentStatus.Completed or IncidentStatus.Failed)
            return;

        incident.MarkFailed(failureReason);
        await incidentRepository.UpdateAsync(incident, cancellationToken);
    }
}
