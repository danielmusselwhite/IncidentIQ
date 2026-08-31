using OpenAI.Chat;

namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Defines the strict JSON Schema supplied to Azure OpenAI for incident analysis.
/// Keeping the schema in one place makes the model contract easy to review and keeps
/// AzureIncidentAnalyzer focused on orchestration.
/// </summary>
internal static class AzureIncidentAnalysisSchema
{
    /// <summary>
    /// Strict Structured Outputs response format used for every incident analysis request.
    /// All properties are required and additional properties are rejected.
    ///
    /// Confidence range validation is also performed in C# after deserialization.
    /// </summary>
    public static ChatResponseFormat ResponseFormat { get; } =
        ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "incident_analysis",
            jsonSchema: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "summary": {
                      "type": "string",
                      "description": "A concise summary of what is most likely happening in the incident."
                    },
                    "likelyCauses": {
                      "type": "array",
                      "description": "Potential technical causes inferred only from the supplied incident information.",
                      "items": {
                        "type": "object",
                        "properties": {
                          "cause": {
                            "type": "string",
                            "description": "A concise description of a plausible technical cause."
                          },
                          "confidence": {
                            "type": "number",
                            "description": "Model-estimated confidence from 0 to 1. This is not a calibrated probability."
                          }
                        },
                        "required": [
                          "cause",
                          "confidence"
                        ],
                        "additionalProperties": false
                      }
                    },
                    "recommendedActions": {
                      "type": "array",
                      "description": "Practical diagnostic or remediation actions for an engineer to review.",
                      "items": {
                        "type": "object",
                        "properties": {
                          "action": {
                            "type": "string",
                            "description": "A concise recommended diagnostic or remediation action."
                          }
                        },
                        "required": [
                          "action"
                        ],
                        "additionalProperties": false
                      }
                    }
                  },
                  "required": [
                    "summary",
                    "likelyCauses",
                    "recommendedActions"
                  ],
                  "additionalProperties": false
                }
                """),
            jsonSchemaFormatDescription:
                "Structured software incident analysis containing a summary, likely causes, and recommended actions.",
            jsonSchemaIsStrict: true);
}
