import {
    useEffect,
    useState,
} from "react";

import {
    Link,
    Navigate,
} from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getFailedIncidents } from "../../api/operationsApi";
import { useCurrentUser } from "../../auth/CurrentUserContext";
import type { FailedIncidentOperation } from
    "../../types/failedIncidentOperation";

import "./OperationsPage.css";

export default function OperationsPage() {
    const {
        isAdministrator,
        isLoading: isUserLoading,
        error: userError,
    } = useCurrentUser();

    const [incidents, setIncidents] =
        useState<FailedIncidentOperation[]>([]);

    const [isLoading, setIsLoading] =
        useState(true);

    const [error, setError] =
        useState<string | null>(null);

    useEffect(() => {
        if (!isAdministrator) {
            setIsLoading(false);
            return;
        }

        async function loadFailedIncidents() {
            try {
                const result =
                    await getFailedIncidents();

                setIncidents(result);
            } catch (loadError) {
                setError(
                    loadError instanceof ApiError
                        ? loadError.message
                        : "Unable to load failed incidents.",
                );
            } finally {
                setIsLoading(false);
            }
        }

        void loadFailedIncidents();
    }, [isAdministrator]);

    if (isUserLoading) {
        return <p>Loading operations...</p>;
    }

    if (userError) {
        return <p>{userError}</p>;
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
        <main className="operations-page">
            <header className="operations-page__header">
                <div>
                    <p className="operations-page__eyebrow">
                        Administration
                    </p>

                    <h1>Operations</h1>

                    <p>
                        Inspect failed Incident analysis
                        operations and recover them when
                        required.
                    </p>
                </div>
            </header>

            <section className="operations-summary">
                <span>Failed analyses</span>
                <strong>{incidents.length}</strong>
            </section>

            <section className="operations-card">
                <div className="operations-card__header">
                    <div>
                        <h2>Failed Incidents</h2>
                        <p>
                            Incidents whose analysis exhausted
                            its processing attempts.
                        </p>
                    </div>
                </div>

                {isLoading && (
                    <div className="operations-state">
                        Loading failed incidents...
                    </div>
                )}

                {error && (
                    <div className="operations-error">
                        {error}
                    </div>
                )}

                {!isLoading &&
                    !error &&
                    incidents.length === 0 && (
                        <div className="operations-state">
                            No failed analyses.
                        </div>
                    )}

                {!isLoading &&
                    !error &&
                    incidents.length > 0 && (
                        <div className="operations-table-wrapper">
                            <table className="operations-table">
                                <thead>
                                    <tr>
                                        <th>Incident</th>
                                        <th>Service</th>
                                        <th>Environment</th>
                                        <th>Severity</th>
                                        <th>Attempts</th>
                                        <th>Failed</th>
                                        <th>Failure reason</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {incidents.map(
                                        incident => (
                                            <tr key={incident.id}>
                                                <td>
                                                    <Link
                                                        to={`/incidents/${incident.id}`}
                                                    >
                                                        {incident.title}
                                                    </Link>
                                                </td>

                                                <td>{incident.service}</td>
                                                <td>{incident.environment}</td>

                                                <td>
                                                    <span
                                                        className={`badge badge--${incident.severity.toLowerCase()}`}
                                                    >
                                                        {incident.severity}
                                                    </span>
                                                </td>

                                                <td>
                                                    {incident.attemptCount}
                                                </td>

                                                <td>
                                                    {formatDate(
                                                        incident.failedAt,
                                                    )}
                                                </td>

                                                <td>
                                                    {incident.failureReason ??
                                                        "Unknown failure"}
                                                </td>
                                            </tr>
                                        ),
                                    )}
                                </tbody>
                            </table>
                        </div>
                    )}
            </section>
        </main>
    );
}

function formatDate(
    value: string | null,
) {
    if (!value) {
        return "—";
    }

    return new Intl.DateTimeFormat(
        "en-GB",
        {
            dateStyle: "medium",
            timeStyle: "short",
        },
    ).format(new Date(value));
}