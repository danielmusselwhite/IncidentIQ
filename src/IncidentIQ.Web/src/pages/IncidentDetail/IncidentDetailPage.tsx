import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getIncident, getIncidentAnalysis } from "../../api/incidentsApi";
import type { Incident } from "../../types/incident";
import type { IncidentAnalysis } from "../../types/incidentAnalysis";

import "./IncidentDetailPage.css";

const POLL_INTERVAL_MS = 2000;

type AnalysisTab = "overview" | "historical" | "runbooks";

/**
 * Determines whether an Incident should continue being polled.
 * Queued and Processing are temporary states, while Completed and Failed are terminal.
 */
function shouldPoll(status: Incident["status"]) {
    return status === "Queued" || status === "Processing";
}

/**
 * Resolves a request-scoped evidence reference such as HI-1 or RB-2 to
 * its persisted evidence snapshot and source route.
 */
function getEvidenceDetails(
    referenceId: string,
    analysis: IncidentAnalysis,
) {
    const historicalIncident =
        analysis.evidence.historicalIncidents.find(
            item => item.referenceId === referenceId,
        );

    if (historicalIncident) {
        return {
            referenceId,
            title: historicalIncident.title,
            kind: "Historical Incident",
            preview: historicalIncident.description,
            href: `/incidents/${historicalIncident.incidentId}`,
        };
    }

    const runbookChunk =
        analysis.evidence.runbookChunks.find(
            item => item.referenceId === referenceId,
        );

    if (runbookChunk) {
        return {
            referenceId,
            title: runbookChunk.title,
            kind: `Runbook · Chunk ${runbookChunk.chunkIndex}`,
            preview: runbookChunk.content,
            href: `/runbooks/${runbookChunk.runbookId}`,
        };
    }

    return null;
}

/**
 * Displays the details of a single Incident and, once processing completes,
 * its persisted grounded AI analysis.
 */
export default function IncidentDetailPage() {
    const { id } = useParams<{ id: string }>();

    const [incident, setIncident] = useState<Incident | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [analysis, setAnalysis] = useState<IncidentAnalysis | null>(null);
    const [analysisError, setAnalysisError] = useState<string | null>(null);
    const [isAnalysisLoading, setIsAnalysisLoading] = useState(false);
    const [activeAnalysisTab, setActiveAnalysisTab] =
        useState<AnalysisTab>("overview");

    /**
     * Loads the Incident when the route ID changes and continues polling while
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

        setAnalysis(null);
        setAnalysisError(null);
        setError(null);
        setActiveAnalysisTab("overview");

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
                        const loadedAnalysis =
                            await getIncidentAnalysis(id!);

                        if (isCancelled) {
                            return;
                        }

                        setAnalysis(loadedAnalysis);
                    } catch {
                        if (!isCancelled) {
                            setAnalysisError(
                                "Unable to load incident analysis.",
                            );
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

                if (
                    caughtError instanceof ApiError &&
                    caughtError.status === 404
                ) {
                    setError("Incident not found.");
                    return;
                }

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
                <p className="incident-detail__state">
                    Loading incident...
                </p>
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

    const historicalCount =
        analysis?.evidence.historicalIncidents.length ?? 0;

    const runbookCount =
        analysis?.evidence.runbookChunks.length ?? 0;

    return (
        <main className="incident-detail">
            <div className="incident-detail__toolbar">
                <Link to="/incidents">← Back to incidents</Link>
            </div>

            <header className="incident-detail__hero">
                <div className="incident-detail__hero-main">
                    <div className="incident-detail__badges">
                        <span
                            className={`badge badge--${incident.severity.toLowerCase()}`}
                        >
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
                        {incident.id}
                    </p>
                </div>

                <dl className="incident-detail__hero-metadata">
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
            </header>

            <section className="incident-context-grid">
                <article className="incident-context-card">
                    <span className="incident-context-card__label">
                        Description
                    </span>

                    <p>{incident.description}</p>
                </article>

                <article className="incident-context-card">
                    <span className="incident-context-card__label">
                        Symptoms
                    </span>

                    <p>
                        {incident.symptoms ||
                            "No symptoms provided."}
                    </p>
                </article>
            </section>

            <section className="incident-analysis">
                <header className="incident-analysis__header">
                    <div className="incident-analysis__heading">
                        <div
                            className="incident-analysis__icon"
                            aria-hidden="true"
                        >
                            AI
                        </div>

                        <div>
                            <span className="incident-analysis__eyebrow">
                                Grounded analysis
                            </span>
                            <h2>Incident Analysis</h2>
                        </div>
                    </div>

                    {analysis && (
                        <div className="incident-analysis__stats">
                            <div>
                                <strong>{historicalCount}</strong>
                                <span>Historical</span>
                            </div>

                            <div>
                                <strong>{runbookCount}</strong>
                                <span>Runbook chunks</span>
                            </div>
                        </div>
                    )}
                </header>

                {incident.status === "Queued" && (
                    <AnalysisStatus
                        title="Waiting for analysis"
                        description="The Incident has been queued and will be analysed shortly."
                    />
                )}

                {incident.status === "Processing" && (
                    <AnalysisStatus
                        title="Analysis in progress"
                        description="IncidentIQ is retrieving operational context and analysing the Incident."
                        processing
                    />
                )}

                {incident.status === "Failed" && (
                    <AnalysisStatus
                        title="Analysis failed"
                        description="The Incident could not be analysed successfully."
                        failed
                    />
                )}

                {incident.status === "Completed" &&
                    isAnalysisLoading && (
                        <AnalysisStatus
                            title="Loading analysis"
                            description="Retrieving the completed grounded analysis."
                            processing
                        />
                    )}

                {incident.status === "Completed" &&
                    analysisError && (
                        <AnalysisStatus
                            title="Unable to load analysis"
                            description={analysisError}
                            failed
                        />
                    )}

                {incident.status === "Completed" && analysis && (
                    <>
                        <nav
                            className="analysis-tabs"
                            aria-label="Incident analysis sections"
                        >
                            <button
                                type="button"
                                className={
                                    activeAnalysisTab === "overview"
                                        ? "analysis-tab analysis-tab--active"
                                        : "analysis-tab"
                                }
                                onClick={() =>
                                    setActiveAnalysisTab("overview")
                                }
                            >
                                Overview
                            </button>

                            {historicalCount > 0 && (
                                <button
                                    type="button"
                                    className={
                                        activeAnalysisTab ===
                                            "historical"
                                            ? "analysis-tab analysis-tab--active"
                                            : "analysis-tab"
                                    }
                                    onClick={() =>
                                        setActiveAnalysisTab(
                                            "historical",
                                        )
                                    }
                                >
                                    Historical Incidents
                                    <span>{historicalCount}</span>
                                </button>
                            )}

                            {runbookCount > 0 && (
                                <button
                                    type="button"
                                    className={
                                        activeAnalysisTab ===
                                            "runbooks"
                                            ? "analysis-tab analysis-tab--active"
                                            : "analysis-tab"
                                    }
                                    onClick={() =>
                                        setActiveAnalysisTab(
                                            "runbooks",
                                        )
                                    }
                                >
                                    Runbooks
                                    <span>{runbookCount}</span>
                                </button>
                            )}
                        </nav>

                        <div className="incident-analysis__content">
                            {activeAnalysisTab === "overview" && (
                                <AnalysisOverview
                                    analysis={analysis}
                                />
                            )}

                            {activeAnalysisTab ===
                                "historical" && (
                                    <HistoricalEvidence
                                        analysis={analysis}
                                    />
                                )}

                            {activeAnalysisTab === "runbooks" && (
                                <RunbookEvidence
                                    analysis={analysis}
                                />
                            )}
                        </div>

                        <footer className="incident-analysis__meta">
                            <span>
                                Model: {analysis.model}
                            </span>

                            <span>
                                Analysed{" "}
                                {formatDate(
                                    analysis.analysedAtUtc,
                                )}
                            </span>
                        </footer>
                    </>
                )}
            </section>
        </main>
    );
}

function AnalysisOverview({
    analysis,
}: {
    analysis: IncidentAnalysis;
}) {
    return (
        <div className="analysis-overview">
            <section className="analysis-summary">
                <span className="analysis-section-label">
                    Summary
                </span>

                <p>{analysis.summary}</p>
            </section>

            <div className="analysis-overview__grid">
                <section className="analysis-panel">
                    <div className="analysis-panel__heading">
                        <div>
                            <span className="analysis-section-label">
                                Diagnosis
                            </span>
                            <h3>Likely Causes</h3>
                        </div>

                        <span className="analysis-panel__count">
                            {analysis.likelyCauses.length}
                        </span>
                    </div>

                    <div className="analysis-causes">
                        {analysis.likelyCauses.map(
                            (cause, index) => (
                                <article
                                    key={index}
                                    className="analysis-cause"
                                >
                                    <div className="analysis-cause__header">
                                        <div className="analysis-cause__number">
                                            {index + 1}
                                        </div>

                                        <strong>
                                            {cause.cause}
                                        </strong>

                                        <span className="analysis-cause__confidence">
                                            {Math.round(
                                                cause.confidence *
                                                100,
                                            )}
                                            %
                                        </span>
                                    </div>

                                    <EvidenceReferences
                                        references={
                                            cause.evidenceReferences
                                        }
                                        analysis={analysis}
                                    />
                                </article>
                            ),
                        )}
                    </div>
                </section>

                <section className="analysis-panel">
                    <div className="analysis-panel__heading">
                        <div>
                            <span className="analysis-section-label">
                                Response
                            </span>
                            <h3>Recommended Actions</h3>
                        </div>

                        <span className="analysis-panel__count">
                            {
                                analysis.recommendedActions
                                    .length
                            }
                        </span>
                    </div>

                    <ol className="analysis-actions">
                        {analysis.recommendedActions.map(
                            (action, index) => (
                                <li key={index}>
                                    <div className="analysis-action__content">
                                        <strong>
                                            {action.action}
                                        </strong>

                                        <EvidenceReferences
                                            references={
                                                action.evidenceReferences
                                            }
                                            analysis={analysis}
                                        />
                                    </div>
                                </li>
                            ),
                        )}
                    </ol>
                </section>
            </div>
        </div>
    );
}

function EvidenceReferences({
    references,
    analysis,
}: {
    references: string[];
    analysis: IncidentAnalysis;
}) {
    if (references.length === 0) {
        return null;
    }

    return (
        <div className="analysis-evidence-references">
            <span className="analysis-evidence-label">
                Supporting evidence
            </span>

            <div className="analysis-evidence-chips">
                {references.map(reference => {
                    const evidence = getEvidenceDetails(
                        reference,
                        analysis,
                    );

                    if (!evidence) {
                        return (
                            <span
                                key={reference}
                                className="analysis-evidence-chip"
                            >
                                {reference}
                            </span>
                        );
                    }

                    return (
                        <Link
                            key={reference}
                            to={evidence.href}
                            className="analysis-evidence-chip analysis-evidence-chip--linked"
                            aria-label={`Open ${evidence.kind} ${evidence.title}`}
                        >
                            <span className="analysis-evidence-chip__reference">
                                {evidence.referenceId}
                            </span>

                            <span className="analysis-evidence-chip__title">
                                {evidence.title}
                            </span>

                            <span
                                className="analysis-evidence-preview"
                                role="tooltip"
                            >
                                <span className="analysis-evidence-preview__type">
                                    {evidence.kind}
                                </span>

                                <strong>
                                    {evidence.title}
                                </strong>

                                <span>
                                    {truncate(
                                        evidence.preview,
                                        220,
                                    )}
                                </span>

                                <span className="analysis-evidence-preview__cta">
                                    Open source →
                                </span>
                            </span>
                        </Link>
                    );
                })}
            </div>
        </div>
    );
}

function HistoricalEvidence({
    analysis,
}: {
    analysis: IncidentAnalysis;
}) {
    return (
        <section className="analysis-evidence-view">
            <div className="analysis-view-heading">
                <div>
                    <span className="analysis-section-label">
                        Retrieved context
                    </span>
                    <h3>Similar Historical Incidents</h3>
                </div>

                <p>
                    Incidents retrieved during the original
                    analysis and preserved as evidence snapshots.
                </p>
            </div>

            <div className="historical-evidence-grid">
                {analysis.evidence.historicalIncidents.map(
                    item => (
                        <Link
                            key={item.referenceId}
                            to={`/incidents/${item.incidentId}`}
                            className="historical-evidence-card"
                            aria-label={`View historical incident ${item.title}`}
                        >
                            <div className="evidence-card__topline">
                                <span className="evidence-reference evidence-reference--historical">
                                    {item.referenceId}
                                </span>

                                <span className="evidence-card__date">
                                    {formatDate(
                                        item.completedAtUtc,
                                    )}
                                </span>
                            </div>

                            <h4>{item.title}</h4>

                            <p>
                                {truncate(
                                    item.description,
                                    210,
                                )}
                            </p>

                            {item.symptoms && (
                                <div className="evidence-card__symptoms">
                                    <span>Symptoms</span>
                                    <p>
                                        {truncate(
                                            item.symptoms,
                                            150,
                                        )}
                                    </p>
                                </div>
                            )}

                            <div className="evidence-card__meta">
                                <span>{item.service}</span>
                                <span>{item.environment}</span>
                                <span>{item.severity}</span>
                            </div>

                            <span className="evidence-card__open">
                                View Incident →
                            </span>
                        </Link>
                    ),
                )}
            </div>
        </section>
    );
}

function RunbookEvidence({
    analysis,
}: {
    analysis: IncidentAnalysis;
}) {
    return (
        <section className="analysis-evidence-view">
            <div className="analysis-view-heading">
                <div>
                    <span className="analysis-section-label">
                        Operational guidance
                    </span>
                    <h3>Runbook Evidence</h3>
                </div>

                <p>
                    Relevant Runbook excerpts supplied to the AI
                    when this Incident was analysed.
                </p>
            </div>

            <div className="runbook-evidence-list">
                {analysis.evidence.runbookChunks.map(item => (
                    <Link
                        key={item.referenceId}
                        to={`/runbooks/${item.runbookId}`}
                        className="runbook-evidence-card"
                        aria-label={`View runbook ${item.title}`}
                    >
                        <div className="runbook-evidence-card__side">
                            <span className="evidence-reference evidence-reference--runbook">
                                {item.referenceId}
                            </span>

                            <span>
                                Chunk {item.chunkIndex}
                            </span>
                        </div>

                        <div className="runbook-evidence-card__body">
                            <div className="runbook-evidence-card__heading">
                                <div>
                                    <span>{item.service}</span>
                                    <h4>{item.title}</h4>
                                </div>

                                <span className="evidence-card__open">
                                    View Runbook →
                                </span>
                            </div>

                            <p>
                                {truncate(item.content, 360)}
                            </p>
                        </div>
                    </Link>
                ))}
            </div>
        </section>
    );
}

function AnalysisStatus({
    title,
    description,
    processing = false,
    failed = false,
}: {
    title: string;
    description: string;
    processing?: boolean;
    failed?: boolean;
}) {
    return (
        <div
            className={
                failed
                    ? "incident-analysis__status incident-analysis__status--failed"
                    : "incident-analysis__status"
            }
        >
            {!failed && (
                <span
                    className={
                        processing
                            ? "incident-analysis__status-dot incident-analysis__status-dot--processing"
                            : "incident-analysis__status-dot"
                    }
                />
            )}

            <div>
                <strong>{title}</strong>
                <p>{description}</p>
            </div>
        </div>
    );
}

function truncate(value: string, maxLength: number) {
    if (value.length <= maxLength) {
        return value;
    }

    return `${value.slice(0, maxLength).trimEnd()}…`;
}

/**
 * Converts an ISO/date string from the API into a readable UK date and time.
 */
function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}