using OpenAI.Chat;

namespace IncidentIQ.Infrastructure.AzureAI.Assistant;

/// <summary>
/// Defines the strict structured-output schema expected from the Azure OpenAI
/// Operational Assistant.
/// </summary>
internal static class AzureOperationalAssistantSchema
{
    public static ChatResponseFormat ResponseFormat { get; } =
        ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "operational_assistant_response",
            jsonSchema: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "sections": {
                      "type": "array",
                      "description": "One or more concise sections forming the grounded operational answer.",
                      "items": {
                        "type": "object",
                        "properties": {
                          "content": {
                            "type": "string",
                            "description": "A concise part of the operational answer."
                          },
                          "evidenceReferences": {
                            "type": "array",
                            "description": "References to supplied evidence that materially supports this section. Use an empty array when no supplied evidence supports the statement.",
                            "items": {
                              "type": "string"
                            }
                          }
                        },
                        "required": [
                          "content",
                          "evidenceReferences"
                        ],
                        "additionalProperties": false
                      }
                    }
                  },
                  "required": [
                    "sections"
                  ],
                  "additionalProperties": false
                }
                """),
            jsonSchemaFormatDescription:
                "A grounded operational answer divided into sections with supporting evidence references.",
            jsonSchemaIsStrict: true);
}