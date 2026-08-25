import type {
    CreateRunbookRequest,
    Runbook,
    UpdateRunbookRequest,
} from "../types/runbook";
import { ApiError, type ApiProblemDetails } from "./apiError";

const apiBaseUrl =
    import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7156";

/**
 * Retrieves all runbooks from the API.
 *
 * @returns A promise that resolves to an array of runbooks.
 * @throws An ApiError if the request fails.
 */
export async function getRunbooks(): Promise<Runbook[]> {
    const response = await fetch(`${apiBaseUrl}/api/runbooks`);

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}

/**
 * Retrieves a specific runbook by its ID.
 *
 * @param id The ID of the runbook to retrieve.
 * @returns A promise that resolves to the requested runbook.
 * @throws An ApiError if the request fails.
 */
export async function getRunbook(id: string): Promise<Runbook> {
    const response = await fetch(`${apiBaseUrl}/api/runbooks/${id}`);

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}

/**
 * Creates a new runbook.
 *
 * @param request The data required to create the runbook.
 * @returns A promise that resolves to the created runbook.
 * @throws An ApiError if the request fails.
 */
export async function createRunbook(
    request: CreateRunbookRequest,
): Promise<Runbook> {
    const response = await fetch(`${apiBaseUrl}/api/runbooks`, {
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

/**
 * Updates an existing runbook.
 *
 * @param id The ID of the runbook to update.
 * @param request The updated runbook values.
 * @returns A promise that resolves to the updated runbook.
 * @throws An ApiError if the request fails.
 */
export async function updateRunbook(
    id: string,
    request: UpdateRunbookRequest,
): Promise<Runbook> {
    const response = await fetch(`${apiBaseUrl}/api/runbooks/${id}`, {
        method: "PUT",
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

/**
 * Deletes a runbook by its ID.
 *
 * @param id The ID of the runbook to delete.
 * @throws An ApiError if the request fails.
 */
export async function deleteRunbook(id: string): Promise<void> {
    const response = await fetch(`${apiBaseUrl}/api/runbooks/${id}`, {
        method: "DELETE",
    });

    if (!response.ok) {
        await throwApiError(response);
    }
}

/**
 * Converts an unsuccessful API response into an ApiError.
 *
 * ASP.NET Core Problem Details responses are parsed where available so
 * callers can access the status code and any validation errors.
 */
async function throwApiError(response: Response): Promise<never> {
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