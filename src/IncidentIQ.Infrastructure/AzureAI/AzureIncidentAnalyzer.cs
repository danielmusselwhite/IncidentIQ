using IncidentIQ.Application.Analyse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;

namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Azure OpenAI implementation of <see cref="IIncidentAnalyzer"/>.
/// Converts an application-level incident input into chat messages, asks the
/// configured Azure OpenAI deployment for a strict structured response, validates
/// that response, and maps it back to the provider-independent Application model.
/// </summary>
public sealed class AzureIncidentAnalyzer(
    ChatClient chatClient,
    IOptions<AzureAIOptions> options,
    ILogger<AzureIncidentAnalyzer> logger)
    : IIncidentAnalyzer
{
    private readonly AzureAIOptions _options = options.Value;
    private readonly ILogger<AzureIncidentAnalyzer> _logger = logger;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Generates a structured AI analysis for the supplied incident.
    /// </summary>
    /// <remarks>
    /// Azure-specific request/response types remain inside Infrastructure.
    /// Caller cancellation is preserved so Worker shutdown can propagate normally.
    /// Azure AI failures are classified before being rethrown so the existing
    /// Worker retry/DLQ flow can continue handling delivery-level retries.
    /// </remarks>
    public async Task<IncidentAnalysisResult> AnalyzeIncidentAsync(
        IncidentAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        #region Validate Input

        ArgumentNullException.ThrowIfNull(input);

        #endregion

        var stopwatch = Stopwatch.StartNew();

        #region Build Azure AI Request

        // Convert the provider-independent incident input into the messages sent to Azure OpenAI.
        var messages = BuildMessages(input);

        // Structured Outputs constrain the model to the schema expected by IncidentIQ.
        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = AzureIncidentAnalysisSchema.ResponseFormat
        };

        #endregion

        #region Call Azure AI

        // Link our overall request timeout to the caller's cancellation token.
        // This allows us to distinguish an IncidentIQ timeout from genuine Worker shutdown.
        using var timeoutCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutCancellationTokenSource.CancelAfter(
            TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

        ChatCompletion completion;

        try
        {
            var completionResponse = await chatClient.CompleteChatAsync(
                messages,
                completionOptions,
                timeoutCancellationTokenSource.Token);

            completion = completionResponse.Value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller explicitly cancelled the operation, for example because
            // the Worker is shutting down. This is not an Azure AI failure.
            throw;
        }
        catch (OperationCanceledException exception)
            when (timeoutCancellationTokenSource.IsCancellationRequested)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.Timeout, exception);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.Timeout,
                $"Azure AI incident analysis exceeded the configured {_options.RequestTimeoutSeconds} second timeout.",
                exception);
        }
        catch (ClientResultException exception) when (exception.Status == 429)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.Throttled, exception);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.Throttled,
                "Azure AI throttled the incident analysis request.",
                exception);
        }
        catch (ClientResultException exception)
            when (exception.Status == 408 ||
                  exception.Status == 0 ||
                  exception.Status >= 500)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.ServiceFailure, exception);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.ServiceFailure,
                $"Azure AI encountered a transient service failure. HTTP status: {exception.Status}.",
                exception);
        }
        catch (ClientResultException exception)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.ClientFailure, exception);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.ClientFailure,
                $"Azure AI rejected the incident analysis request. HTTP status: {exception.Status}.",
                exception);
        }

        #endregion

        #region Extract Model Response

        // A successful request should contain structured JSON in the first content item.
        if (completion.Content.Count == 0 ||
            string.IsNullOrWhiteSpace(completion.Content[0].Text))
        {
            LogFailure(stopwatch, AzureAIFailureCategory.InvalidResponse);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.InvalidResponse,
                "Azure AI returned an empty incident analysis response.");
        }

        var responseJson = completion.Content[0].Text;

        #endregion

        #region Deserialize Structured Response

        AzureIncidentAnalysisResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<AzureIncidentAnalysisResponse>(
                responseJson,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.InvalidResponse, exception);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.InvalidResponse,
                "Azure AI returned an incident analysis response that could not be deserialized.",
                exception);
        }

        if (response is null)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.InvalidResponse);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.InvalidResponse,
                "Azure AI returned a null incident analysis response.");
        }

        #endregion

        #region Validate AI Response

        try
        {
            // Structured Outputs validate the JSON shape. This additionally checks
            // semantic constraints such as required values and confidence ranges.
            response.Validate();
        }
        catch (Exception exception)
            when (exception is InvalidOperationException or ArgumentException)
        {
            LogFailure(stopwatch, AzureAIFailureCategory.InvalidResponse, exception);

            throw new AzureAIAnalysisException(
                AzureAIFailureCategory.InvalidResponse,
                "Azure AI returned an incident analysis response that failed semantic validation.",
                exception);
        }

        #endregion

        #region Map To Application Result

        // Convert the Azure-specific response model into the provider-independent
        // result understood by the Application layer.
        var result = new IncidentAnalysisResult(
            Summary: response.Summary,
            LikelyCauses: response.LikelyCauses
                .Select(cause => new LikelyCause(
                    cause.Cause,
                    cause.Confidence))
                .ToList(),
            RecommendedActions: response.RecommendedActions
                .Select(action => new RecommendedAction(
                    action.Action))
                .ToList(),
            Model: _options.ModelName,
            AnalysedAtUtc: DateTimeOffset.UtcNow);

        #endregion

        #region Record Successful Analysis

        stopwatch.Stop();

        _logger.LogInformation(
            "Azure AI incident analysis completed successfully in {DurationMs} ms using deployment {DeploymentName} and model {ModelName}.",
            stopwatch.ElapsedMilliseconds,
            _options.DeploymentName,
            _options.ModelName);

        #endregion

        return result;
    }

    /// <summary>
    /// Builds the conversation sent to the model.
    /// The system message defines behaviour and grounding rules, while the
    /// user message contains only the incident-specific data to analyse.
    /// </summary>
    private static IReadOnlyList<ChatMessage> BuildMessages(
        IncidentAnalysisInput input)
    {
        return
        [
            new SystemChatMessage(
                """
                You are an incident analysis assistant for software production systems.

                Analyse only the incident information supplied by the user.
                Produce a concise summary, likely technical causes, and practical investigation or remediation actions.

                Rules:
                - Do not claim access to runbooks, historical incidents, monitoring data, logs, metrics, deployments, or evidence that was not supplied.
                - Treat likely causes as hypotheses, not confirmed facts.
                - Confidence values must be numbers between 0 and 1, where 0 means very low confidence and 1 means very high confidence.
                - Recommend safe diagnostic or remediation steps that are appropriate for an engineer to review.
                - Do not invent identifiers, links, commands, incidents, or runbook references.
                - Return content matching the required structured response schema.
                """),

            new UserChatMessage(
                $"""
                Analyse the following incident.

                Title: {input.Title}
                Description: {input.Description}
                Service: {input.Service}
                Environment: {input.Environment}
                Severity: {input.Severity}
                Symptoms: {input.Symptoms ?? "Not provided"}
                """)
        ];
    }

    /// <summary>
    /// Records a classified Azure AI failure together with its duration and
    /// deployment metadata without logging incident content or model output.
    /// </summary>
    /// <param name="stopwatch">Measures how long the analysis ran before failing.</param>
    /// <param name="category">The classified reason for the failure.</param>
    /// <param name="exception">The exception that caused the failure, when available.</param>
    private void LogFailure(
        Stopwatch stopwatch,
        AzureAIFailureCategory category,
        Exception? exception = null)
    {
        stopwatch.Stop();

        _logger.LogWarning(
            exception,
            "Azure AI incident analysis failed after {DurationMs} ms. Category: {FailureCategory}. Deployment: {DeploymentName}. Model: {ModelName}.",
            stopwatch.ElapsedMilliseconds,
            category,
            _options.DeploymentName,
            _options.ModelName);
    }
}