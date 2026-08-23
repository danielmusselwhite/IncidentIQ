import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getIncident } from "../../api/incidentsApi";
import type { Incident } from "../../types/incident";

import "./IncidentDetailPage.css";

export default function IncidentDetailPage() {
    const { id } = useParams<{ id: string }>();

    const [incident, setIncident] = useState<Incident | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!id) {
            setError("Incident ID is missing.");
            setIsLoading(false);
            return;
        }

        const loadIncident = async () => {
            try {
                const result = await getIncident(id);
                setIncident(result);
            } catch (error) {
                if (error instanceof ApiError && error.status === 404) {
                    setError("Incident not found.");
                } else {
                    setError("Unable to load incident.");
                }
            } finally {
                setIsLoading(false);
            }
        };

        void loadIncident();
    }, [id]);

    if (isLoading) {
        return (
            <main className="incident-detail">
                <p className="incident-detail__state">Loading incident...</p>
            </main>
        );
    }

    if (error || !incident) {
        return (
            <main className="incident-detail">
                <div className="incident-detail__error">
                    <h1>Unable to display incident</h1>
                    <p>{error ?? "Incident not found."}</p>

                    <Link to="/incidents">Back to incidents</Link>
                </div>
            </main>
        );
    }

    return (
        <main className="incident-detail">
            <div className="incident-detail__toolbar">
                <Link to="/incidents">← Back to incidents</Link>
            </div>

            <header className="incident-detail__header">
                <div>
                    <div className="incident-detail__badges">
                        <span className={`badge badge--${incident.severity.toLowerCase()}`}>
                            {incident.severity}
                        </span>

                        <span className="badge badge--status">
                            {incident.status}
                        </span>
                    </div>

                    <h1>{incident.title}</h1>

                    <p className="incident-detail__id">
                        Incident ID: {incident.id}
                    </p>
                </div>
            </header>

            <section className="incident-detail__card">
                <h2>Incident Details</h2>

                <dl className="incident-detail__metadata">
                    <div>
                        <dt>Service</dt>
                        <dd>{incident.service}</dd>
                    </div>

                    <div>
                        <dt>Environment</dt>
                        <dd>{incident.environment}</dd>
                    </div>

                    <div>
                        <dt>Created</dt>
                        <dd>{formatDate(incident.createdAt)}</dd>
                    </div>

                    <div>
                        <dt>Updated</dt>
                        <dd>{formatDate(incident.updatedAt)}</dd>
                    </div>
                </dl>
            </section>

            <section className="incident-detail__card">
                <h2>Description</h2>
                <p>{incident.description}</p>
            </section>

            <section className="incident-detail__card">
                <h2>Symptoms</h2>
                <p>{incident.symptoms || "No symptoms provided."}</p>
            </section>

            <section className="incident-detail__card incident-detail__analysis">
                <h2>Analysis</h2>

                <p>
                    AI analysis has not been implemented yet. This section will
                    display likely causes, recommended actions and supporting
                    evidence once asynchronous analysis is added.
                </p>
            </section>
        </main>
    );
}

function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}