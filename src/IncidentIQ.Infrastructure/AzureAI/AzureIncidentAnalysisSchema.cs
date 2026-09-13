using OpenAI.Chat;

namespace IncidentIQ.Infrastructure.AzureAI;

/// <summary>
/// Defines the strict JSON Schema supplied to Azure OpenAI for Incident analysis.
/// Keeping the schema in one place makes the model contract easy to review and keeps
/// AzureIncidentAnalyzer focused on orchestration.
/// </summary>
internal static class AzureIncidentAnalysisSchema
{
  /// <summary>
  /// Strict Structured Outputs response format used for every Incident analysis request.
  /// All properties are required and additional properties are rejected.
  ///
  /// Semantic validation, including confidence ranges and evidence-reference validity,
  /// is also performed in C# after deserialization.
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
                      "description": "A concise summary of what is most likely happening in the Incident."
                    },
                    "likelyCauses": {
                      "type": "array",
                      "description": "Potential technical causes inferred from the current Incident and supplied grounding evidence.",
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
                          },
                          "evidenceReferences": {
                            "type": "array",
                            "description": "References such as HI-1 or RB-2 identifying supplied evidence that materially supports this cause. Use an empty array when no retrieved evidence supports it.",
                            "items": {
                              "type": "string"
                            }
                          }
                        },
                        "required": [
                          "cause",
                          "confidence",
                          "evidenceReferences"
                        ],
                        "additionalProperties": false
                      }
                    },
                    "recommendedActions": {
                      "type": "array",
                      "description": "Practical diagnostic or remediation actions inferred from the current Incident and supplied grounding evidence.",
                      "items": {
                        "type": "object",
                        "properties": {
                          "action": {
                            "type": "string",
                            "description": "A concise recommended diagnostic or remediation action."
                          },
                          "evidenceReferences": {
                            "type": "array",
                            "description": "References such as HI-1 or RB-2 identifying supplied evidence that materially supports this action. Use an empty array when no retrieved evidence supports it.",
                            "items": {
                              "type": "string"
                            }
                          }
                        },
                        "required": [
                          "action",
                          "evidenceReferences"
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
              "Structured grounded software Incident analysis containing a summary, likely causes, recommended actions, and supporting evidence references.",
          jsonSchemaIsStrict: true);
}