using IncidentIQ.Application.Analyse;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
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
    IOptions<AzureAIOptions> options)
    : IIncidentAnalyzer
{
    private readonly AzureAIOptions _options = options.Value;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Generates a structured AI analysis for the supplied incident.
    /// </summary>
    /// <remarks>
    /// Azure-specific request/response types remain inside Infrastructure.
    /// Cancellation is deliberately not caught so Worker shutdown can propagate
    /// normally. Azure SDK failures also propagate so the existing Worker
    /// retry/DLQ flow can handle them.
    /// </remarks>
    public async Task<IncidentAnalysisResult> AnalyzeIncidentAsync(
    IncidentAnalysisInput input,
    CancellationToken cancellationToken = default)
{
        #region Validate Input
        // Fail immediately if the caller somehow supplies a null input.
        ArgumentNullException.ThrowIfNull(input);
        #endregion

        #region Build Azure AI Request
        // Convert the IncidentAnalysisInput into the system/user chat messages that will be sent to the Azure OpenAI model.
        var messages = BuildMessages(input);

        // Configure the model to return Structured Output. This means Azure AI must return JSON matching the schema defined in AzureIncidentAnalysisSchema instead of returning arbitrary prose.
        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = AzureIncidentAnalysisSchema.ResponseFormat
        };
        #endregion

        #region Call Azure AI
        // Send the messages to the configured Azure OpenAI deployment. The injected ChatClient already knows which deployment to call.
        var completionResponse = await chatClient.CompleteChatAsync(
            messages,
            completionOptions,
            cancellationToken);

        // Azure SDK methods commonly wrap the actual result in an Azure Response<T>. Value extracts the ChatCompletion returned by the model.
        var completion = completionResponse.Value;
        #endregion

        #region Extract Model Response
        // A successful request should contain at least one content item containing the structured JSON produced by the model.
        if (completion.Content.Count == 0 ||
            string.IsNullOrWhiteSpace(completion.Content[0].Text))
        {
            throw new InvalidOperationException(
                "Azure AI returned an empty incident analysis response.");
        }

        var responseJson = completion.Content[0].Text;
        #endregion

        #region Deserialize Structured Response
        AzureIncidentAnalysisResponse? response;

        try
        {
            // Convert the JSON returned by Azure AI into our Infrastructure-specific response model.
            response = JsonSerializer.Deserialize<AzureIncidentAnalysisResponse>(
                responseJson,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            // Structured Outputs should normally prevent malformed JSON, but we still protect the application in case an invalid/unexpected response is returned.
            throw new InvalidOperationException(
                "Azure AI returned an incident analysis response that could not be deserialized.",
                exception);
        }

        if (response is null)
        {
            throw new InvalidOperationException(
                "Azure AI returned a null incident analysis response.");
        }
        #endregion

        #region Validate AI Response
        // JSON Schema validates the shape of the response, while this performs additional semantic checks
        response.Validate();
        #endregion

        #region Map To Application Result
        // Convert the Azure-specific response model into the provider-independent IncidentAnalysisResult understood by the Application layer.
        return new IncidentAnalysisResult(
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
    }

    /// <summary>
    /// Builds the conversation sent to the model.
    /// The system message defines behaviour and safety/grounding rules, while
    /// the user message contains only the incident-specific data to analyse.
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
}
