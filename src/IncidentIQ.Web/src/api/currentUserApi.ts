import { apiFetch, throwApiError } from "./apiClient";
import type { CurrentUser } from "../types/currentUser";

export async function getCurrentUser(): Promise<CurrentUser> {
    const response = await apiFetch("/api/me");

    if (!response.ok) {
        return throwApiError(response);
    }

    return response.json() as Promise<CurrentUser>;
}