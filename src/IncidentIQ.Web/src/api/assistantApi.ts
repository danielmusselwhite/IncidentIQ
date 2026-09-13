import type {
    AskOperationalQuestionRequest,
    OperationalAssistantResponse,
} from "../types/assistant";
import { ApiError, type ApiProblemDetails } from "./apiError";

const apiBaseUrl =
    import.meta.env.VITE_API_BASE_URL ??
    "https://localhost:7156";

/**
 * Submits an operational question to the grounded IncidentIQ Assistant.
 *
 * The request can include optional service/environment filters and recent
 * conversation history. The backend retrieves relevant historical Incidents
 * and Runbook evidence before generating the response.
 *
 * @param request The operational question and optional conversational context.
 * @returns A promise that resolves to the grounded Assistant response.
 * @throws An ApiError if the request fails.
 */
export async function askOperationalQuestion(
    request: AskOperationalQuestionRequest,
): Promise<OperationalAssistantResponse> {
    const response = await fetch(
        `${apiBaseUrl}/api/assistant/questions`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}

/**
 * Converts an unsuccessful API response into the shared ApiError type.
 */
async function throwApiError(
    response: Response,
): Promise<never> {
    const problem = await response
        .json()
        .catch(() => null) as ApiProblemDetails | null;

    throw new ApiError(
        problem?.detail ??
            problem?.title ??
            "An unexpected error occurred.",
        response.status,
        problem?.errors,
    );
}