import {
    useEffect,
    useMemo,
    useState,
    type FormEvent,
} from "react";
import { Link } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import {
    getRunbooks,
    searchRunbookChunks,
} from "../../api/runbooksApi";
import type {
    Runbook,
    RunbookChunkMatch,
} from "../../types/runbook";

import "./RunbooksPage.css";

/**
 * Displays the Runbook management page.
 *
 * The page supports:
 * - normal client-side filtering of loaded Runbooks;
 * - semantic vector search over indexed Runbook chunks;
 * - navigation to individual Runbook details.
 */
export default function Runbooks() {
    // Stores the Runbooks returned by the API.
    const [runbooks, setRunbooks] = useState<Runbook[]>([]);

    // Client-side text used to filter the normal Runbook table.
    const [search, setSearch] = useState("");

    // Semantic search state.
    const [semanticQuery, setSemanticQuery] = useState("");
    const [semanticService, setSemanticService] = useState("");
    const [semanticResults, setSemanticResults] =
        useState<RunbookChunkMatch[]>([]);
    const [isSemanticSearching, setIsSemanticSearching] =
        useState(false);
    const [semanticSearchError, setSemanticSearchError] =
        useState<string | null>(null);
    const [hasSemanticSearched, setHasSemanticSearched] =
        useState(false);

    // Tracks whether the initial Runbook request is still in progress.
    const [isLoading, setIsLoading] = useState(true);

    // Stores a user-friendly error message if loading fails.
    const [error, setError] = useState<string | null>(null);

    /**
     * Loads the Runbook list when the component first appears.
     */
    useEffect(() => {
        async function loadRunbooks() {
            try {
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
                setIsLoading(false);
            }
        }

        void loadRunbooks();
    }, []);

    /**
     * Searches indexed Runbook chunks using semantic vector similarity.
     *
     * The query is sent only when the form is submitted so embedding and
     * Cosmos vector-search requests are not generated on every keystroke.
     */
    async function handleSemanticSearch(event: FormEvent) {
        event.preventDefault();

        const query = semanticQuery.trim();
        const service = semanticService.trim();

        if (!query) {
            return;
        }

        try {
            setIsSemanticSearching(true);
            setSemanticSearchError(null);
            setHasSemanticSearched(true);

            const results = await searchRunbookChunks(
                query,
                service || undefined,
                5,
            );

            setSemanticResults(results);
        } catch (error) {
            setSemanticResults([]);

            if (error instanceof ApiError) {
                setSemanticSearchError(error.message);
            } else {
                setSemanticSearchError(
                    "Unable to perform semantic Runbook search.",
                );
            }
        } finally {
            setIsSemanticSearching(false);
        }
    }

    /**
     * Produces the list of Runbooks matching the normal table search.
     *
     * This is a simple client-side text filter and is separate from semantic
     * vector search.
     */
    const filteredRunbooks = useMemo(() => {
        const value = search.trim().toLowerCase();

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

                <Link
                    className="button button--primary"
                    to="/runbooks/new"
                >
                    New Runbook
                </Link>
            </header>

            {/* Semantic vector search over indexed Runbook chunks. */}
            <section className="runbooks-semantic-card">
                <div className="runbooks-semantic-card__header">
                    <div>
                        <p className="runbooks-semantic-card__eyebrow">
                            Semantic Search
                        </p>

                        <h2>Find relevant operational guidance</h2>

                        <p>
                            Describe an incident or symptom in natural language
                            to find semantically related Runbook guidance.
                        </p>
                    </div>
                </div>

                <form
                    className="runbooks-semantic-form"
                    onSubmit={handleSemanticSearch}
                >
                    <input
                        className="runbooks-semantic-form__query"
                        type="search"
                        placeholder="e.g. customers cannot checkout because the payment provider keeps timing out"
                        value={semanticQuery}
                        onChange={(event) =>
                            setSemanticQuery(event.target.value)
                        }
                    />

                    <input
                        className="runbooks-semantic-form__service"
                        type="text"
                        placeholder="Service (optional)"
                        value={semanticService}
                        onChange={(event) =>
                            setSemanticService(event.target.value)
                        }
                    />

                    <button
                        className="button button--primary"
                        type="submit"
                        disabled={
                            isSemanticSearching ||
                            !semanticQuery.trim()
                        }
                    >
                        {isSemanticSearching
                            ? "Searching..."
                            : "Semantic Search"}
                    </button>
                </form>

                {semanticSearchError && (
                    <div className="runbooks-semantic-card__error">
                        {semanticSearchError}
                    </div>
                )}

                {!isSemanticSearching &&
                    !semanticSearchError &&
                    hasSemanticSearched &&
                    semanticResults.length === 0 && (
                        <div className="runbooks-semantic-card__empty">
                            No matching Runbook chunks were found.
                        </div>
                    )}

                {semanticResults.length > 0 && (
                    <div className="runbooks-semantic-results">
                        <div className="runbooks-semantic-results__header">
                            <h3>Relevant Runbook Chunks</h3>

                            <span>
                                {semanticResults.length} result
                                {semanticResults.length === 1 ? "" : "s"}
                            </span>
                        </div>

                        <div className="runbooks-semantic-results__list">
                            {semanticResults.map((result) => (
                                <Link
                                    key={`${result.runbookId}-${result.chunkIndex}`}
                                    className="runbooks-semantic-result"
                                    to={`/runbooks/${result.runbookId}`}
                                >
                                    <div className="runbooks-semantic-result__header">
                                        <div>
                                            <h3>{result.title}</h3>

                                            <span className="runbooks-semantic-result__service">
                                                {result.service}
                                            </span>
                                        </div>

                                        <span className="runbooks-semantic-result__distance">
                                            Distance{" "}
                                            {result.distance.toFixed(3)}
                                        </span>
                                    </div>

                                    <p className="runbooks-semantic-result__content">
                                        {result.content}
                                    </p>

                                    <span className="runbooks-semantic-result__chunk">
                                        Chunk {result.chunkIndex + 1}
                                    </span>
                                </Link>
                            ))}
                        </div>
                    </div>
                )}
            </section>

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
                        placeholder="Filter runbooks..."
                        value={search}
                        onChange={(event) =>
                            setSearch(event.target.value)
                        }
                    />
                </div>

                {isLoading && (
                    <div className="runbooks-page__state">
                        Loading runbooks...
                    </div>
                )}

                {!isLoading && error && (
                    <div className="runbooks-page__error">
                        {error}
                    </div>
                )}

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
 * Displays a single Runbook within the Runbook table.
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
 */
function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
    }).format(new Date(value));
}