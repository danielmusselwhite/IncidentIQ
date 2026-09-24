import type { FailedIncidentOperation } from
    "../types/failedIncidentOperation";
import type { OperationsSummary } from "../types/operationsSummary";

import {
    apiFetch,
    throwApiError,
} from "./apiClient";

export async function getFailedIncidents():
    Promise<FailedIncidentOperation[]> {
    const response =
        await apiFetch(
            "/api/operations/failed-incidents",
        );

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}

export async function retryIncident(
    incidentId: string,
): Promise<void> {
    const response = await apiFetch(
        `/api/incidents/${incidentId}/retry`,
        {
            method: "POST",
        },
    );

    if (!response.ok) {
        await throwApiError(response);
    }
}

export async function getOperationsSummary(): Promise<OperationsSummary> {
    const response = await apiFetch("/api/operations/summary");

    if (!response.ok) {
        await throwApiError(response);
    }

    return response.json();
}