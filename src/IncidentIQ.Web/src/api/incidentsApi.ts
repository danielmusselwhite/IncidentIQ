import type {
    CreateIncidentRequest,
    Incident,
} from "../types/incident";
import type { IncidentAnalysis } from "../types/incidentAnalysis";
import { ApiError, type ApiProblemDetails } from "./apiError";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7156"; // if the environment variable is not set, default to the local development URL

/** Creates a new incident by sending a POST request to the API.
 * @param request The data required to create a new incident.
 * @returns A promise that resolves to the created incident.
 * @throws An error if the request fails.
 */
export async function createIncident(
    request: CreateIncidentRequest,
): Promise<Incident> {
    const response = await fetch(`${apiBaseUrl}/api/incidents`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}

/** Retrieves a list of all incidents from the API.
 * @returns A promise that resolves to an array of incidents.
 * @throws An error if the request fails.
 */
export async function getIncidents(): Promise<Incident[]> {
    const response = await fetch(`${apiBaseUrl}/api/incidents`);

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}

/** Retrieves a specific incident by its ID from the API.
 * @param id The ID of the incident to retrieve.
 * @returns A promise that resolves to the incident.
 * @throws An error if the request fails.
 */
export async function getIncident(id: string): Promise<Incident> {
    const response = await fetch(`${apiBaseUrl}/api/incidents/${id}`);

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}



/** Retrieves the analysis for a specific incident from the API.
 * @param id The ID of the incident to retrieve the analysis for.
 * @returns A promise that resolves to the incident analysis.
 * @throws An error if the request fails.
 */
export async function getIncidentAnalysis(id: string): Promise<IncidentAnalysis> {
    const response = await fetch(`${apiBaseUrl}/api/incidents/${id}/analysis`);

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}


async function throwApiError(response: Response): Promise<never> {
    const problem = await response
        .json()
        .catch(() => null) as ApiProblemDetails | null;

    throw new ApiError(
        problem?.detail ?? problem?.title ?? "An unexpected error occurred.",
        response.status,
        problem?.errors,
    );
}