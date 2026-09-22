import { Navigate } from "react-router-dom";

import { useCurrentUser } from "../../auth/CurrentUserContext";

export default function OperationsPage() {
    const {
        isAdministrator,
        isLoading,
        error,
    } = useCurrentUser();

    if (isLoading) {
        return <p>Loading operations...</p>;
    }

    if (error) {
        return <p>{error}</p>;
    }

    if (!isAdministrator) {
        return (
            <Navigate
                to="/incidents"
                replace
            />
        );
    }

    return (
        <section>
            <h1>Operations</h1>
            <p>
                Monitor and recover failed Incident analysis
                operations.
            </p>
        </section>
    );
}