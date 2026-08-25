import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { getRunbook, updateRunbook } from "../../api/runbooksApi";
import RunbookForm from "../../components/RunbookForm/RunbookForm";
import type {
    Runbook,
    UpdateRunbookRequest,
} from "../../types/runbook";

import "./EditRunbookPage.css";

/**
 * Displays the page used to edit an existing runbook.
 *
 * The runbook ID is read from the URL and used to load the existing data.
 * That data is then passed into the reusable RunbookForm component.
 *
 * This page is responsible for loading the runbook, handling update requests,
 * displaying errors, and navigating back to the runbook after a successful save.
 */
export default function EditRunbook() {
    // Read the dynamic ":id" value from the current route.
    const { id } = useParams<{ id: string }>();

    // Allows the page to navigate programmatically after a successful update.
    const navigate = useNavigate();

    // Stores the runbook loaded from the API.
    const [runbook, setRunbook] = useState<Runbook | null>(null);

    // Tracks the initial loading request.
    const [isLoading, setIsLoading] = useState(true);

    // Tracks whether an update request is currently in progress.
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Stores validation errors returned by the API, grouped by field name.
    const [fieldErrors, setFieldErrors] = useState<
        Record<string, string[]>
    >({});

    // Stores errors that occur while initially loading the runbook.
    const [loadError, setLoadError] = useState<string | null>(null);

    // Stores general errors that occur while saving changes.
    const [submitError, setSubmitError] = useState<string | null>(null);

    // Kept separately so a missing runbook can have a dedicated UI state.
    const [notFound, setNotFound] = useState(false);

    /**
     * Loads the runbook whenever the route ID changes.
     */
    useEffect(() => {
        /**
         * Retrieves the runbook from the API and stores it in component state.
         */
        async function loadRunbook() {
            // Guard against a missing route parameter before making the request.
            if (!id) {
                setNotFound(true);
                setIsLoading(false);
                return;
            }

            try {
                // Reset any previous state before loading a different runbook.
                setIsLoading(true);
                setLoadError(null);
                setNotFound(false);
                setRunbook(null);

                const result = await getRunbook(id);

                setRunbook(result);
            } catch (error) {
                // Handle a missing runbook separately from other API failures.
                if (error instanceof ApiError && error.status === 404) {
                    setNotFound(true);
                    return;
                }

                setLoadError("Unable to load runbook.");
            } finally {
                // Stop displaying the loading state regardless of the outcome.
                setIsLoading(false);
            }
        }

        // useEffect itself cannot be async, so the async work is placed
        // inside loadRunbook() instead.
        void loadRunbook();
    }, [id]);

    /**
     * Submits the edited runbook values to the API.
     *
     * If successful, the user is redirected back to the updated runbook.
     * Validation errors are passed back into RunbookForm for display.
     *
     * @param request - Updated runbook values entered by the user.
     */
    async function handleSubmit(request: UpdateRunbookRequest) {
        // The route should contain an ID, but avoid making an invalid request
        // if the component somehow renders without one.
        if (!id) {
            return;
        }

        try {
            // Clear errors from previous submission attempts.
            setIsSubmitting(true);
            setFieldErrors({});
            setSubmitError(null);

            const updatedRunbook = await updateRunbook(id, request);

            // Return to the detail page for the updated runbook.
            navigate(`/runbooks/${updatedRunbook.id}`);
        } catch (error) {
            if (error instanceof ApiError) {
                // Store any field-level validation errors returned by the API.
                setFieldErrors(error.errors);

                // If there are no field-specific errors, display the API
                // message as a general submission error instead.
                if (Object.keys(error.errors).length === 0) {
                    setSubmitError(error.message);
                }

                return;
            }

            // Handle unexpected errors outside the normal API error format.
            setSubmitError("Unable to update runbook.");
        } finally {
            // Re-enable the form once the request has finished.
            setIsSubmitting(false);
        }
    }

    // Return early while the existing runbook is being loaded.
    if (isLoading) {
        return (
            <main className="edit-runbook">
                <div className="edit-runbook__state">
                    Loading runbook...
                </div>
            </main>
        );
    }

    // Show a dedicated state when the requested runbook does not exist.
    if (notFound) {
        return (
            <main className="edit-runbook">
                <div className="edit-runbook__state">
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

    // Handle other load failures, or the unexpected case where no
    // runbook exists despite loading finishing successfully.
    if (loadError || !runbook) {
        return (
            <main className="edit-runbook">
                <div className="edit-runbook__state edit-runbook__state--error">
                    {loadError ?? "Unable to load runbook."}
                </div>
            </main>
        );
    }

    // At this point loading succeeded, so TypeScript knows runbook is not null.
    return (
        <main className="edit-runbook">
            <header className="edit-runbook__header">
                <Link
                    className="edit-runbook__back"
                    to={`/runbooks/${runbook.id}`}
                >
                    ← Back to Runbook
                </Link>

                <h1>Edit Runbook</h1>

                <p>
                    Update the operational guidance for this runbook.
                </p>
            </header>

            <section className="edit-runbook__form">
                <RunbookForm
                    /*
                     * Pre-populate the reusable form with the existing
                     * runbook values so the user can edit them.
                     */
                    initialValues={{
                        title: runbook.title,
                        description: runbook.description,
                        service: runbook.service,
                        content: runbook.content,
                    }}
                    submitLabel="Save Changes"
                    isSubmitting={isSubmitting}
                    fieldErrors={fieldErrors}
                    submitError={submitError}
                    onCancel={() => navigate(`/runbooks/${runbook.id}`)}
                    onSubmit={(request) =>
                        /*
                         * RunbookForm supports both create and update requests.
                         * This page only performs updates, so we tell TypeScript
                         * to treat the submitted values as UpdateRunbookRequest.
                         */
                        handleSubmit(request as UpdateRunbookRequest)
                    }
                />
            </section>
        </main>
    );
}