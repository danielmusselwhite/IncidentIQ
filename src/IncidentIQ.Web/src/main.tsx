import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { MsalProvider } from "@azure/msal-react";

import App from "./App";
import { msalInstance } from "./auth/msalInstance";

import "./index.css";

// Application bootstrap: Initializes authentication, restores any existing user session, and mounts the React application once everything is ready.
async function bootstrap() {
    // Initialize the MSAL authentication client before the app starts.
    await msalInstance.initialize();

    // Check whether MSAL has any accounts from a previous login/session.
    const accounts = msalInstance.getAllAccounts();

    // If a user is already signed in, make the first account the active account used by the application for authentication.
    if (accounts.length > 0) {
        msalInstance.setActiveAccount(accounts[0]);
    }

    // Mount the React application and establish the top-level providers required by the application.
    createRoot(document.getElementById("root")!).render(
        <StrictMode>
            {/* Makes the MSAL authentication instance available throughout the app. */}
            <MsalProvider instance={msalInstance}>
                {/* Enables client-side routing throughout the application. */}
                <BrowserRouter>
                    {/* Root application component. */}
                    <App />
                </BrowserRouter>
            </MsalProvider>
        </StrictMode>,
    );
}

// Start the application bootstrap process.
void bootstrap();