import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getRunbooks } from "../../api/runbooksApi";
import type { Runbook } from "../../types/runbook";

import "./RunbooksPage.css";

/**
 * Displays the runbook management page.
 *
 * The page loads all runbooks from the API, allows the user to search them,
 * and displays the available runbooks in a table.
 */
export default function Runbooks() {
    // Stores the runbooks returned by the API.
    const [runbooks, setRunbooks] = useState<Runbook[]>([]);

    // Stores the current search value entered by the user.
    const [search, setSearch] = useState("");

    // Tracks whether the initial API request is still in progress.
    const [isLoading, setIsLoading] = useState(true);

    // Stores a user-friendly error message if loading fails.
    const [error, setError] = useState<string | null>(null);

    /**
     * Loads the runbook list when the component first appears.
     *
     * The empty dependency array means this effect only runs when the
     * component is first mounted.
     */
    useEffect(() => {
        /**
         * Retrieves runbooks from the API and updates the page state.
         */
        async function loadRunbooks() {
            try {
                // Reset the page state before starting the request.
                setIsLoading(true);
                setError(null);

                const result = await getRunbooks();

                setRunbooks(result);
            } catch (error) {
                if (error instanceof ApiError) {
                    setError(error.message);
                } else {
                    setError("Unable to load runbooks.");
                }
            } finally {
                // Stop showing the loading state regardless of success/failure.
                setIsLoading(false);
            }
        }

        // useEffect itself cannot be async, so the asynchronous work is
        // performed by loadRunbooks() instead.
        void loadRunbooks();
    }, []);

    /**
     * Produces the list of runbooks matching the current search text.
     *
     * Runbooks can be searched by title, service, description, or content.
     */
    const filteredRunbooks = useMemo(() => {
        // Normalise the value so matching is case-insensitive and leading
        // or trailing whitespace does not affect the result.
        const value = search.trim().toLowerCase();

        // An empty search should display all runbooks.
        if (!value) {
            return runbooks;
        }

        return runbooks.filter((runbook) =>
            [
                runbook.title,
                runbook.service,
                runbook.description,
                runbook.content,
            ].some((field) =>
                field.toLowerCase().includes(value),
            ),
        );
    }, [runbooks, search]);

    return (
        <main className="runbooks-page">
            <header className="runbooks-page__header">
                <div>
                    <p className="runbooks-page__eyebrow">
                        Knowledge Management
                    </p>

                    <h1>Runbooks</h1>

                    <p>
                        Manage operational guidance used to investigate and
                        resolve incidents.
                    </p>
                </div>

                {/* Navigate to the form for creating a new runbook. */}
                <Link
                    className="button button--primary"
                    to="/runbooks/new"
                >
                    New Runbook
                </Link>
            </header>

            <section className="runbooks-table-card">
                <div className="runbooks-table-card__header">
                    <div>
                        <h2>Operational Runbooks</h2>

                        <p>
                            Guidance available for incident investigation and
                            resolution.
                        </p>
                    </div>

                    <input
                        className="runbooks-search"
                        type="search"
                        placeholder="Search runbooks..."
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                    />
                </div>

                {/* Show while the API request is still running. */}
                {isLoading && (
                    <div className="runbooks-page__state">
                        Loading runbooks...
                    </div>
                )}

                {/* Show an error only after loading has finished. */}
                {!isLoading && error && (
                    <div className="runbooks-page__error">
                        {error}
                    </div>
                )}

                {/*
                 * Show an empty state when loading succeeds but there are
                 * either no runbooks or no runbooks matching the search.
                 */}
                {!isLoading &&
                    !error &&
                    filteredRunbooks.length === 0 && (
                        <div className="runbooks-page__empty">
                            <h3>
                                {runbooks.length === 0
                                    ? "No runbooks yet"
                                    : "No runbooks found"}
                            </h3>

                            <p>
                                {runbooks.length === 0
                                    ? "Create your first runbook to add operational knowledge."
                                    : "No runbooks match your search."}
                            </p>

                            {runbooks.length === 0 && (
                                <Link
                                    className="button button--primary"
                                    to="/runbooks/new"
                                >
                                    Create Runbook
                                </Link>
                            )}
                        </div>
                    )}

                {/*
                 * Only render the table once loading is complete, there is no
                 * error, and at least one runbook matches the current search.
                 */}
                {!isLoading &&
                    !error &&
                    filteredRunbooks.length > 0 && (
                        <div className="runbooks-table-container">
                            <table className="runbooks-table">
                                <thead>
                                    <tr>
                                        <th>Runbook</th>
                                        <th>Service</th>
                                        <th>Description</th>
                                        <th>Updated</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {filteredRunbooks.map((runbook) => (
                                        <RunbookRow
                                            key={runbook.id}
                                            runbook={runbook}
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
 * Displays a single runbook within the runbook table.
 *
 * @param runbook - Runbook data to display.
 */
function RunbookRow({ runbook }: { runbook: Runbook }) {
    return (
        <tr>
            <td>
                <Link
                    className="runbooks-table__title"
                    to={`/runbooks/${runbook.id}`}
                >
                    {runbook.title}
                </Link>
            </td>

            <td className="runbooks-table__service">
                {runbook.service}
            </td>

            <td className="runbooks-table__description">
                {runbook.description}
            </td>

            <td className="runbooks-table__updated">
                {formatDate(runbook.updatedAt)}
            </td>
        </tr>
    );
}

/**
 * Converts a date string returned by the API into a readable UK date.
 *
 * @param value - Date string to format.
 * @returns The formatted date.
 */
function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
    }).format(new Date(value));
}