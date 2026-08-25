import { useEffect, useState } from "react";
import {
    Link,
    useNavigate,
    useParams,
} from "react-router-dom";

import { ApiError } from "../../api/apiError";
import {
    deleteRunbook,
    getRunbook,
} from "../../api/runbooksApi";
import type { Runbook } from "../../types/runbook";

import "./RunbookDetailPage.css";

/**
 * Displays the details of a single runbook.
 *
 * The runbook ID is read from the current URL, then used to load the
 * corresponding runbook from the API.
 *
 * The component handles loading, not-found, error, delete, and successful
 * states.
 */
export default function RunbookDetailPage() {
    // Read the dynamic ":id" value from the current route.
    const { id } = useParams<{ id: string }>();

    // Allows navigation after successfully deleting the runbook.
    const navigate = useNavigate();

    // Stores the runbook returned by the API.
    const [runbook, setRunbook] = useState<Runbook | null>(null);

    // Tracks whether the initial API request is currently in progress.
    const [isLoading, setIsLoading] = useState(true);

    // Tracks whether the delete request is currently in progress.
    const [isDeleting, setIsDeleting] = useState(false);

    // Stores a general error message if loading fails.
    const [error, setError] = useState<string | null>(null);

    // Stores an error specifically related to deleting the runbook.
    const [deleteError, setDeleteError] = useState<string | null>(null);

    // Kept separately from a general error so the page can show a more
    // specific message when the requested runbook does not exist.
    const [notFound, setNotFound] = useState(false);

    /**
     * Loads the runbook whenever the route ID changes.
     */
    useEffect(() => {
        /**
         * Retrieves the requested runbook from the API and updates page state.
         */
        async function loadRunbook() {
            // Guard against a missing route parameter before making the request.
            if (!id) {
                setNotFound(true);
                setIsLoading(false);
                return;
            }

            try {
                // Reset state in case the component is reused for a different ID.
                setIsLoading(true);
                setError(null);
                setNotFound(false);
                setRunbook(null);

                const result = await getRunbook(id);

                setRunbook(result);
            } catch (err) {
                // Treat a 404 separately so the user gets a clear
                // "not found" message rather than a generic error.
                if (err instanceof ApiError && err.status === 404) {
                    setNotFound(true);
                    return;
                }

                setError("Unable to load runbook.");
            } finally {
                // Stop showing the loading state regardless of the outcome.
                setIsLoading(false);
            }
        }

        // useEffect itself cannot be async, so the async work is performed
        // inside loadRunbook() instead.
        void loadRunbook();
    }, [id]);

    /**
     * Deletes the current runbook after receiving confirmation from the user.
     *
     * After a successful deletion, the user is returned to the Runbooks page.
     */
    async function handleDelete() {
        if (!runbook) {
            return;
        }

        const confirmed = window.confirm(
            `Delete "${runbook.title}"? This action cannot be undone.`,
        );

        if (!confirmed) {
            return;
        }

        try {
            setIsDeleting(true);
            setDeleteError(null);

            await deleteRunbook(runbook.id);

            navigate("/runbooks");
        } catch {
            setDeleteError("Unable to delete runbook.");
        } finally {
            setIsDeleting(false);
        }
    }

    // Return early while the API request is in progress.
    if (isLoading) {
        return (
            <main className="runbook-detail">
                <div className="runbook-detail__state">
                    Loading runbook...
                </div>
            </main>
        );
    }

    // Show a dedicated state when the requested runbook does not exist.
    if (notFound) {
        return (
            <main className="runbook-detail">
                <div className="runbook-detail__state">
                    <h1>Runbook not found</h1>

                    <p>
                        The requested runbook does not exist or may have been
                        deleted.
                    </p>

                    <Link
                        className="button button--primary"
                        to="/runbooks"
                    >
                        Back to Runbooks
                    </Link>
                </div>
            </main>
        );
    }

    // If there was another error, or no runbook was returned unexpectedly,
    // show a general failure state instead of rendering incomplete data.
    if (error || !runbook) {
        return (
            <main className="runbook-detail">
                <div className="runbook-detail__state runbook-detail__state--error">
                    <p>{error ?? "Unable to load runbook."}</p>

                    <Link
                        className="button button--secondary"
                        to="/runbooks"
                    >
                        Back to Runbooks
                    </Link>
                </div>
            </main>
        );
    }

    return (
        <main className="runbook-detail">
            <header className="runbook-detail__header">
                <div className="runbook-detail__heading">
                    <Link
                        className="runbook-detail__back"
                        to="/runbooks"
                    >
                        ← Back to Runbooks
                    </Link>

                    <h1>{runbook.title}</h1>

                    <p className="runbook-detail__description">
                        {runbook.description}
                    </p>
                </div>

                <div className="runbook-detail__actions">
                    <Link
                        className="button button--secondary"
                        to={`/runbooks/${runbook.id}/edit`}
                    >
                        Edit
                    </Link>

                    <button
                        className="button button--danger"
                        type="button"
                        disabled={isDeleting}
                        onClick={() => void handleDelete()}
                    >
                        {isDeleting ? "Deleting..." : "Delete"}
                    </button>
                </div>
            </header>

            {deleteError && (
                <div className="runbook-detail__delete-error">
                    {deleteError}
                </div>
            )}

            <section className="runbook-detail__metadata">
                <MetadataItem
                    label="Service"
                    value={runbook.service}
                />

                <MetadataItem
                    label="Created"
                    value={formatDate(runbook.createdAt)}
                />

                <MetadataItem
                    label="Updated"
                    value={formatDate(runbook.updatedAt)}
                />
            </section>

            <section className="runbook-detail__content">
                <div className="runbook-detail__content-header">
                    <h2>Runbook</h2>

                    <p>
                        Operational guidance for incident investigation and
                        resolution.
                    </p>
                </div>

                <div className="runbook-detail__content-body">
                    <pre>{runbook.content}</pre>
                </div>
            </section>
        </main>
    );
}

/**
 * Displays a single piece of runbook metadata.
 *
 * @param label - Description of the metadata value.
 * @param value - Metadata value to display.
 */
function MetadataItem({
    label,
    value,
}: {
    label: string;
    value: string;
}) {
    return (
        <div className="runbook-detail__metadata-item">
            <span className="runbook-detail__label">
                {label}
            </span>

            <strong className="runbook-detail__value">
                {value}
            </strong>
        </div>
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