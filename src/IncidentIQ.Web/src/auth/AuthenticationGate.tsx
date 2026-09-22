import type { ReactNode } from "react";
import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import LoginPage from "./LoginPage";

type AuthenticationGateProps = {
    children: ReactNode;
};

export default function AuthenticationGate({
    children,
}: AuthenticationGateProps) {
    const isAuthenticated = useIsAuthenticated();
    const { inProgress } = useMsal();

    // if the authentication process is still in progress, show a loading message
    if (inProgress !== InteractionStatus.None) {
        return (
            <main className="login-page">
                <section className="login-card">
                    <div className="login-card__logo">IQ</div>
                    <p>Signing you in...</p>
                </section>
            </main>
        );
    }

    // if the user is not authenticated, show the login page
    if (!isAuthenticated) {
        return <LoginPage />;
    }

    // if the user is authenticated, render the protected content
    return <>{children}</>;
}