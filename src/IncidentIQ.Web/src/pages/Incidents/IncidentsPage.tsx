import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getIncidents } from "../../api/incidentsApi";
import type { Incident } from "../../types/incident";

import "./IncidentsPage.css";

/**
 * Displays the main incident dashboard.
 *
 * The page loads incidents from the API, shows summary statistics,
 * allows the user to search/filter incidents, and renders the results
 * in a table.
 */
export default function IncidentsPage() {
    // Stores the incidents returned by the API.
    const [incidents, setIncidents] = useState<Incident[]>([]);

    // Stores the current value entered into the search box.
    const [search, setSearch] = useState("");

    // Tracks whether the initial API request is still in progress.
    const [isLoading, setIsLoading] = useState(true);

    // Stores an error message if loading incidents fails.
    const [error, setError] = useState<string | null>(null);

    /**
     * Loads the incident list when the component is first mounted.
     *
     * The empty dependency array means this effect runs once when the
     * page first appears, rather than after every render.
     */
    useEffect(() => {
        /**
         * Retrieves all incidents from the API and stores them in state.
         */
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
                // Stop displaying the loading state whether the request
                // succeeded or failed.
                setIsLoading(false);
            }
        };

        // useEffect itself cannot be async, so the asynchronous work is
        // placed inside loadIncidents() and called from the effect.
        void loadIncidents();
    }, []);

    /**
     * Produces the list of incidents matching the current search text.
     *
     * useMemo remembers the calculated result and only recalculates it
     * when either the incident list or search value changes.
     */
    const filteredIncidents = useMemo(() => {
        // Normalise the search value so matching is case-insensitive and
        // leading/trailing spaces do not affect the result.
        const value = search.trim().toLowerCase();

        // An empty search should display the full incident list.
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
            ].some(
                // some() returns true when at least one searchable field
                // contains the user's search value.
                (field) => field.toLowerCase().includes(value),
            ),
        );
    }, [incidents, search]);

    // Calculate the dashboard statistics from the current incident list.
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

                {/* Link performs client-side navigation without refreshing the page. */}
                <Link
                    to="/incidents/new"
                    className="button button--primary"
                >
                    Submit Incident
                </Link>
            </header>

            <section className="incident-stats">
                {/* Reuse the StatCard component for each dashboard statistic. */}
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
                        // This is a controlled input: React state holds the
                        // current value and is updated whenever the user types.
                        onChange={(event) => setSearch(event.target.value)}
                    />
                </div>

                {/* Render the loading message while the API request is running. */}
                {isLoading && (
                    <div className="incidents-page__state">
                        Loading incidents...
                    </div>
                )}

                {/* Render an error message if the API request failed. */}
                {error && (
                    <div className="incidents-page__error">
                        {error}
                    </div>
                )}

                {/*
                 * Once loading has finished successfully, show an empty state
                 * when there are no incidents matching the current search.
                 */}
                {!isLoading &&
                    !error &&
                    filteredIncidents.length === 0 && (
                        <div className="incidents-page__empty">
                            <h3>No incidents found</h3>

                            <p>
                                {/*
                                 * Use a different message depending on whether
                                 * there are no incidents at all, or simply no
                                 * incidents matching the user's search.
                                 */}
                                {incidents.length === 0
                                    ? "Submit your first incident to begin analysis."
                                    : "No incidents match your search."}
                            </p>
                        </div>
                    )}

                {/*
                 * Only display the table once loading is complete, there is no
                 * error, and at least one incident matches the current search.
                 */}
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
                                    {/*
                                     * map() converts each Incident object into
                                     * an IncidentRow React component.
                                     *
                                     * key gives React a stable identifier for
                                     * each row so it can efficiently update
                                     * the list when data changes.
                                     */}
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

/**
 * Displays a single dashboard statistic.
 *
 * @param label - Description of the statistic.
 * @param value - Numeric value to display.
 */
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

/**
 * Displays a single incident as a row within the incident table.
 *
 * The incident title links to that incident's detail page.
 *
 * @param incident - Incident data to display in the row.
 */
function IncidentRow({ incident }: { incident: Incident }) {
    return (
        <tr>
            <td>
                <Link
                    // Template literals allow the incident ID to be inserted
                    // into the route dynamically.
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
                    // Build the CSS class from the severity.
                    // For example, "Critical" becomes "badge--critical".
                    className={`badge badge--${incident.severity.toLowerCase()}`}
                >
                    {incident.severity}
                </span>
            </td>

            <td>
                <span
                    className={`badge badge--status badge--status-${incident.status.toLowerCase()}`}
                >
                    {incident.status}
                </span>
            </td>

            <td>{formatDate(incident.createdAt)}</td>
        </tr>
    );
}

/**
 * Converts a date string returned by the API into a readable UK date/time.
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