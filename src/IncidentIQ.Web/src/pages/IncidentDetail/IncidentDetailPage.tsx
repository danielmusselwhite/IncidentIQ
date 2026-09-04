import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getIncident, getIncidentAnalysis } from "../../api/incidentsApi";
import type { Incident } from "../../types/incident";
import type { IncidentAnalysis } from "../../types/incidentAnalysis";

import "./IncidentDetailPage.css";

const POLL_INTERVAL_MS = 2000;

/**
 * Determines whether an incident should continue being polled.
 * Queued and Processing are temporary states, while Completed and Failed are terminal.
 */
function shouldPoll(status: Incident["status"]) {
    return status === "Queued" || status === "Processing";
}

/**
 * Displays the details of a single incident and, once processing completes,
 * its persisted AI-generated analysis.
 *
 * The incident ID is read from the URL, for example:
 * /incidents/123 -> id = "123"
 */
export default function IncidentDetailPage() {
    const { id } = useParams<{ id: string }>();

    const [incident, setIncident] = useState<Incident | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [analysis, setAnalysis] = useState<IncidentAnalysis | null>(null);
    const [analysisError, setAnalysisError] = useState<string | null>(null);
    const [isAnalysisLoading, setIsAnalysisLoading] = useState(false);

    /**
     * Loads the incident when the route ID changes and continues polling while
     * the asynchronous analysis workflow is still Queued or Processing.
     */
    useEffect(() => {
        if (!id) {
            setError("Incident ID is missing.");
            setIsLoading(false);
            return;
        }

        let isCancelled = false;
        let pollTimeout: number | undefined;

        // Reset route-specific state when moving between incidents.
        setAnalysis(null);
        setAnalysisError(null);
        setError(null);

        async function loadIncident(isInitialLoad: boolean) {
            try {
                if (isInitialLoad) {
                    setIsLoading(true);
                }

                const loadedIncident = await getIncident(id!);

                if (isCancelled) {
                    return;
                }

                setIncident(loadedIncident);
                setError(null);

                if (shouldPoll(loadedIncident.status)) {
                    pollTimeout = window.setTimeout(
                        () => void loadIncident(false),
                        POLL_INTERVAL_MS,
                    );

                    return;
                }

                if (loadedIncident.status === "Completed") {
                    setIsAnalysisLoading(true);
                    setAnalysisError(null);

                    try {
                        const loadedAnalysis = await getIncidentAnalysis(id!);

                        if (isCancelled) {
                            return;
                        }

                        setAnalysis(loadedAnalysis);
                    } catch {
                        if (!isCancelled) {
                            setAnalysisError("Unable to load incident analysis.");
                        }
                    } finally {
                        if (!isCancelled) {
                            setIsAnalysisLoading(false);
                        }
                    }
                }
            } catch (caughtError) {
                if (isCancelled) {
                    return;
                }

                if (caughtError instanceof ApiError && caughtError.status === 404) {
                    setError("Incident not found.");
                    return;
                }

                /*
                 * An initial failure prevents the page from loading.
                 * A polling failure keeps the already-loaded incident visible
                 * and retries after the polling interval.
                 */
                if (isInitialLoad) {
                    setError("Unable to load incident.");
                } else {
                    pollTimeout = window.setTimeout(
                        () => void loadIncident(false),
                        POLL_INTERVAL_MS,
                    );
                }
            } finally {
                if (!isCancelled && isInitialLoad) {
                    setIsLoading(false);
                }
            }
        }

        void loadIncident(true);

        // Prevent pending polling or API responses from updating state after navigation.
        return () => {
            isCancelled = true;

            if (pollTimeout !== undefined) {
                window.clearTimeout(pollTimeout);
            }
        };
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

                        <span
                            className={`badge badge--status badge--status-${incident.status.toLowerCase()}`}
                        >
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
                <div className="incident-analysis__header">
                    <div className="incident-analysis__icon" aria-hidden="true">
                        AI
                    </div>

                    <div>
                        <span className="incident-analysis__eyebrow">AI Analysis</span>
                        <h2>Incident Analysis</h2>
                    </div>
                </div>

                {incident.status === "Queued" && (
                    <div className="incident-analysis__status">
                        <span className="incident-analysis__status-dot" />
                        <div>
                            <strong>Waiting for analysis</strong>
                            <p>The incident has been queued and will be analysed shortly.</p>
                        </div>
                    </div>
                )}

                {incident.status === "Processing" && (
                    <div className="incident-analysis__status">
                        <span className="incident-analysis__status-dot incident-analysis__status-dot--processing" />
                        <div>
                            <strong>Analysis in progress</strong>
                            <p>IncidentIQ is currently analysing the incident.</p>
                        </div>
                    </div>
                )}

                {incident.status === "Failed" && (
                    <div className="incident-analysis__status incident-analysis__status--failed">
                        <div>
                            <strong>Analysis failed</strong>
                            <p>The incident could not be analysed successfully.</p>
                        </div>
                    </div>
                )}

                {incident.status === "Completed" && isAnalysisLoading && (
                    <div className="incident-analysis__status">
                        <span className="incident-analysis__status-dot incident-analysis__status-dot--processing" />
                        <div>
                            <strong>Loading analysis</strong>
                            <p>Retrieving the completed analysis result.</p>
                        </div>
                    </div>
                )}

                {incident.status === "Completed" && analysisError && (
                    <div className="incident-analysis__status incident-analysis__status--failed">
                        <div>
                            <strong>Unable to load analysis</strong>
                            <p>{analysisError}</p>
                        </div>
                    </div>
                )}

                {incident.status === "Completed" && analysis && (
                    <div className="incident-analysis__content">
                        <section className="analysis-section">
                            <h3>Summary</h3>
                            <p>{analysis.summary}</p>
                        </section>

                        <section className="analysis-section">
                            <h3>Likely Causes</h3>

                            <div className="analysis-causes">
                                {analysis.likelyCauses.map((cause, index) => (
                                    <div key={index} className="analysis-cause">
                                        <div className="analysis-cause__header">
                                            <strong>{cause.cause}</strong>

                                            <span className="analysis-cause__confidence">
                                                {Math.round(cause.confidence * 100)}%
                                            </span>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </section>

                        <section className="analysis-section">
                            <h3>Recommended Actions</h3>

                            <ol className="analysis-actions">
                                {analysis.recommendedActions.map((action, index) => (
                                    <li key={index}>{action.action}</li>
                                ))}
                            </ol>
                        </section>

                        <footer className="incident-analysis__meta">
                            <span>Model: {analysis.model}</span>
                            <span>Analysed {formatDate(analysis.analysedAtUtc)}</span>
                        </footer>
                    </div>
                )}
            </section>
        </main>
    );
}

/**
 * Converts an ISO/date string from the API into a readable UK date and time.
 *
 * Example:
 * "2026-08-23T14:30:00Z" -> "23 Aug 2026, 15:30"
 */
function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}
