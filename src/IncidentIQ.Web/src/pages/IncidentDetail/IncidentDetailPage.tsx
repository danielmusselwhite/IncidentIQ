import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getIncident } from "../../api/incidentsApi";
import type { Incident } from "../../types/incident";

import "./IncidentDetailPage.css";

/**
 * Displays the details of a single incident.
 *
 * The incident ID is read from the URL, for example:
 * /incidents/123 -> id = "123"
 *
 * When the component loads, the incident is fetched from the API and the page
 * displays either a loading state, an error state, or the incident details.
 */
export default function IncidentDetailPage() {
    // useParams reads dynamic values from the current route.
    // The generic tells TypeScript that this route may contain an "id" parameter.
    const { id } = useParams<{ id: string }>();

    // React state used by this component.
    // Updating any of these values causes React to re-render the component.
    const [incident, setIncident] = useState<Incident | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    /**
     * Loads the incident whenever the route's incident ID changes.
     *
     * useEffect is used because fetching data is a side effect: it interacts
     * with something outside of rendering the component itself.
     */
    useEffect(() => {
        // The route should always provide an ID, but guard against a malformed
        // or incorrectly configured route before attempting an API request.
        if (!id) {
            setError("Incident ID is missing.");
            setIsLoading(false);
            return;
        }

        /**
         * Fetches the incident from the API and updates the component state.
         */
        const loadIncident = async () => {
            try {
                const result = await getIncident(id);
                setIncident(result);
            } catch (error) {
                // Give the user a more specific message when the API explicitly
                // reports that the requested incident does not exist.
                if (error instanceof ApiError && error.status === 404) {
                    setError("Incident not found.");
                } else {
                    setError("Unable to load incident.");
                }
            } finally {
                // finally runs whether the request succeeded or failed.
                setIsLoading(false);
            }
        };

        // Effects themselves cannot be async, so the async work is placed in
        // loadIncident() and invoked from within the effect.
        // "void" indicates that we intentionally do not use the returned Promise.
        void loadIncident();
    }, [id]); // Re-run this effect if the incident ID in the URL changes.

    // Return early while the API request is still in progress.
    // This prevents the main page from rendering before incident data exists.
    if (isLoading) {
        return (
            <main className="incident-detail">
                <p className="incident-detail__state">Loading incident...</p>
            </main>
        );
    }

    // If loading failed, or no incident was returned, show the error state
    // instead of attempting to access properties on a null incident.
    if (error || !incident) {
        return (
            <main className="incident-detail">
                <div className="incident-detail__error">
                    <h1>Unable to display incident</h1>

                    {/* ?? uses the fallback only when error is null or undefined. */}
                    <p>{error ?? "Incident not found."}</p>

                    {/* Link performs client-side navigation without reloading the page. */}
                    <Link to="/incidents">Back to incidents</Link>
                </div>
            </main>
        );
    }

    // At this point loading has completed successfully and TypeScript knows
    // that incident cannot be null.
    return (
        <main className="incident-detail">
            <div className="incident-detail__toolbar">
                <Link to="/incidents">← Back to incidents</Link>
            </div>

            <header className="incident-detail__header">
                <div>
                    <div className="incident-detail__badges">
                        {/*
                         * The severity is included in the CSS class dynamically.
                         * For example, "Critical" becomes "badge--critical".
                         */}
                        <span
                            className={`badge badge--${incident.severity.toLowerCase()}`}
                        >
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

                {/*
                 * A description list (<dl>) is used because these values are
                 * naturally represented as label/value pairs.
                 */}
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

                {/* Use a fallback message when no symptoms were supplied. */}
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

/**
 * Converts an ISO/date string from the API into a readable UK date and time.
 *
 * Example:
 * "2026-08-23T14:30:00Z" -> "23 Aug 2026, 15:30"
 *
 * @param value - Date string to format.
 * @returns The formatted date and time.
 */
function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}