import { useMsal } from "@azure/msal-react";

import { loginRequest } from "./authConfig";

import "./LoginPage.css";

export default function LoginPage() {
    const { instance } = useMsal();

    const handleLogin = async () => {
        await instance.loginRedirect(loginRequest);
    };

    return (
        <main className="login-page">
            <section className="login-card">
                <div className="login-card__logo">IQ</div>

                <h1>IncidentIQ</h1>

                <p>
                    Sign in with your Microsoft account to access the
                    IncidentIQ engineering workspace.
                </p>

                <button
                    type="button"
                    className="button button--primary"
                    onClick={handleLogin}
                >
                    Sign in with Microsoft
                </button>
            </section>
        </main>
    );
}