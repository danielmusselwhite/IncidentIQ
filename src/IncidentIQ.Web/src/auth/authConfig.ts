import type { Configuration, RedirectRequest } from "@azure/msal-browser";

function requireEnvironmentVariable(name: string, value?: string): string {
    if (!value) {
        throw new Error(`${name} is not configured.`);
    }

    return value;
}

const tenantId = requireEnvironmentVariable(
    "VITE_ENTRA_TENANT_ID",
    import.meta.env.VITE_ENTRA_TENANT_ID,
);

const clientId = requireEnvironmentVariable(
    "VITE_ENTRA_CLIENT_ID",
    import.meta.env.VITE_ENTRA_CLIENT_ID,
);

export const apiScope = requireEnvironmentVariable(
    "VITE_API_SCOPE",
    import.meta.env.VITE_API_SCOPE,
);

export const msalConfig: Configuration = {
    auth: {
        clientId,
        authority: `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: window.location.origin,
        postLogoutRedirectUri: window.location.origin,
    },
    cache: {
        cacheLocation: "sessionStorage",
    },
};

export const loginRequest: RedirectRequest = {
    scopes: [apiScope],
};