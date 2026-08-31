using IncidentIQ.Application.Common.Abstractions;
using IncidentIQ.Application.Common.Exceptions;
using IncidentIQ.Domain.Incidents;

namespace IncidentIQ.Application.Analyse;

public sealed class AnalyseIncidentHandler(IIncidentRepository incidentRepository, IIncidentAnalyzer incidentAnalyzer, IIncidentAnalysisStore incidentAnalysisStore)
{
    /// <summary>
    /// Handles the analysis of an incident based on the provided command. It retrieves the incident from the repository, checks its status, and performs the analysis if it hasn't been completed yet. If the incident is already completed, it treats the operation as a no-op to ensure idempotency.
    /// </summary>
    /// <param name="command">The command containing the incident ID to analyze.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="IncidentNotFoundException">Thrown if the incident with the specified ID is not found.</exception>
    public async Task HandleAsync(AnalyseIncidentCommand command, CancellationToken cancellationToken = default)
    {
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

        // perform analysis        
        var analysisResult = await incidentAnalyzer.AnalyzeIncidentAsync(
            analysisInput,
            cancellationToken);

        // Do not persist Completed separately: the store atomically commits the completed Incident and its IncidentAnalysis document.
        incident.MarkCompleted();

        await incidentAnalysisStore.StoreCompletedAnalysisAsync(
            incident,
            analysisResult,
            cancellationToken);
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
