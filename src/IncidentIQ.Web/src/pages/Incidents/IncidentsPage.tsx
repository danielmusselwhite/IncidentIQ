import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getIncidents } from "../../api/incidentsApi";
import type { Incident } from "../../types/incident";

import "./IncidentsPage.css";

export default function IncidentsPage() {
    const [incidents, setIncidents] = useState<Incident[]>([]);
    const [search, setSearch] = useState("");
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const loadIncidents = async () => {
            try {
                const result = await getIncidents();
                setIncidents(result);
            } catch (error) {
                if (error instanceof ApiError) {
                    setError(error.message);
                } else {
                    setError("Unable to load incidents.");
                }
            } finally {
                setIsLoading(false);
            }
        };

        void loadIncidents();
    }, []);

    const filteredIncidents = useMemo(() => {
        const value = search.trim().toLowerCase();

        if (!value) {
            return incidents;
        }

        return incidents.filter((incident) =>
            [
                incident.title,
                incident.service,
                incident.environment,
                incident.status,
                incident.severity,
            ].some((field) => field.toLowerCase().includes(value)),
        );
    }, [incidents, search]);

    const activeIncidents = incidents.filter(
        (incident) =>
            incident.status === "Queued" ||
            incident.status === "Processing",
    ).length;

    const criticalIncidents = incidents.filter(
        (incident) => incident.severity === "Critical",
    ).length;

    const completedIncidents = incidents.filter(
        (incident) => incident.status === "Completed",
    ).length;

    return (
        <main className="incidents-page">
            <header className="incidents-page__header">
                <div>
                    <p className="incidents-page__eyebrow">
                        Incident Management
                    </p>

                    <h1>Dashboard</h1>

                    <p>
                        Monitor submitted incidents and track analysis progress.
                    </p>
                </div>

                <Link
                    to="/incidents/new"
                    className="button button--primary"
                >
                    Submit Incident
                </Link>
            </header>

            <section className="incident-stats">
                <StatCard
                    label="Total Incidents"
                    value={incidents.length}
                />

                <StatCard
                    label="Active"
                    value={activeIncidents}
                />

                <StatCard
                    label="Critical"
                    value={criticalIncidents}
                />

                <StatCard
                    label="Completed"
                    value={completedIncidents}
                />
            </section>

            <section className="incidents-table-card">
                <div className="incidents-table-card__header">
                    <div>
                        <h2>Incidents</h2>
                        <p>
                            Recent incidents submitted for investigation.
                        </p>
                    </div>

                    <input
                        className="incidents-search"
                        type="search"
                        placeholder="Search incidents..."
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                    />
                </div>

                {isLoading && (
                    <div className="incidents-page__state">
                        Loading incidents...
                    </div>
                )}

                {error && (
                    <div className="incidents-page__error">
                        {error}
                    </div>
                )}

                {!isLoading &&
                    !error &&
                    filteredIncidents.length === 0 && (
                        <div className="incidents-page__empty">
                            <h3>No incidents found</h3>

                            <p>
                                {incidents.length === 0
                                    ? "Submit your first incident to begin analysis."
                                    : "No incidents match your search."}
                            </p>
                        </div>
                    )}

                {!isLoading &&
                    !error &&
                    filteredIncidents.length > 0 && (
                        <div className="incidents-table-wrapper">
                            <table className="incidents-table">
                                <thead>
                                    <tr>
                                        <th>Incident</th>
                                        <th>Service</th>
                                        <th>Environment</th>
                                        <th>Severity</th>
                                        <th>Status</th>
                                        <th>Created</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {filteredIncidents.map((incident) => (
                                        <IncidentRow
                                            key={incident.id}
                                            incident={incident}
                                        />
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
            </section>
        </main>
    );
}

function StatCard({
    label,
    value,
}: {
    label: string;
    value: number;
}) {
    return (
        <div className="incident-stat">
            <span>{label}</span>
            <strong>{value}</strong>
        </div>
    );
}

function IncidentRow({ incident }: { incident: Incident }) {
    return (
        <tr>
            <td>
                <Link
                    to={`/incidents/${incident.id}`}
                    className="incidents-table__incident"
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
                <span className="badge badge--status">
                    {incident.status}
                </span>
            </td>

            <td>{formatDate(incident.createdAt)}</td>
        </tr>
    );
}

function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}