import { InteractionRequiredAuthError } from "@azure/msal-browser";

import { apiScope } from "../auth/authConfig";
import { msalInstance } from "../auth/msalInstance";
import { ApiError, type ApiProblemDetails } from "./apiError";

const apiBaseUrl =
    import.meta.env.VITE_API_BASE_URL ??
    "https://localhost:7156";

export async function apiFetch(
    path: string,
    init?: RequestInit,
): Promise<Response> {
    let account = msalInstance.getActiveAccount();

    if (!account) {
        const accounts = msalInstance.getAllAccounts();

        if (accounts.length > 0) {
            account = accounts[0];
            msalInstance.setActiveAccount(account);
        }
    }

    if (!account) {
        throw new Error("No authenticated user is available.");
    }

    let accessToken: string;

    try {
        const tokenResult =
            await msalInstance.acquireTokenSilent({
                account,
                scopes: [apiScope],
            });

        accessToken = tokenResult.accessToken;
    } catch (error) {
        if (error instanceof InteractionRequiredAuthError) {
            throw new Error(
                "Your authentication session requires user interaction.",
            );
        }

        throw error;
    }

    const headers = new Headers(init?.headers);

    headers.set(
        "Authorization",
        `Bearer ${accessToken}`,
    );

    const response = await fetch(
        `${apiBaseUrl}${path}`,
        {
            ...init,
            headers,
        },
    );

    return response;
}

export async function throwApiError(
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