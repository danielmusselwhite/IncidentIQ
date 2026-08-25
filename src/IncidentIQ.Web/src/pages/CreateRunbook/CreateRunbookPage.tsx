import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { createRunbook } from "../../api/runbooksApi";
import RunbookForm from "../../components/RunbookForm/RunbookForm";
import type { CreateRunbookRequest } from "../../types/runbook";

import "./CreateRunbookPage.css";

/**
 * Displays the page used to create a new runbook.
 *
 * The reusable RunbookForm component handles the form fields themselves,
 * while this page handles submission, API errors, and navigation after
 * the runbook has been created successfully.
 */
export default function CreateRunbook() {
    // useNavigate allows the component to navigate to another route
    // programmatically after the API request succeeds.
    const navigate = useNavigate();

    // Tracks whether the create request is currently running.
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Stores validation errors returned by the API, grouped by field name.
    const [fieldErrors, setFieldErrors] = useState<
        Record<string, string[]>
    >({});

    // Stores a general submission error not associated with a specific field.
    const [submitError, setSubmitError] = useState<string | null>(null);

    /**
     * Creates a new runbook using the values submitted by RunbookForm.
     *
     * If successful, the user is redirected to the newly created runbook.
     * Validation errors are passed back to the form so they can be displayed
     * next to the appropriate fields.
     *
     * @param request - Runbook values entered by the user.
     */
    async function handleSubmit(request: CreateRunbookRequest) {
        try {
            // Reset state from any previous submission before starting.
            setIsSubmitting(true);
            setFieldErrors({});
            setSubmitError(null);

            const runbook = await createRunbook(request);

            // Navigate to the detail page for the newly created runbook.
            navigate(`/runbooks/${runbook.id}`);
        } catch (error) {
            if (error instanceof ApiError) {
                // Store any field-level validation errors returned by the API.
                setFieldErrors(error.errors);

                /*
                 * If there are no field-specific errors, display the API
                 * message as a general form-level error instead.
                 */
                if (Object.keys(error.errors).length === 0) {
                    setSubmitError(error.message);
                }

                return;
            }

            // Handle unexpected errors that do not use the normal ApiError format.
            setSubmitError("Unable to create runbook.");
        } finally {
            // Re-enable the form regardless of whether creation succeeded or failed.
            setIsSubmitting(false);
        }
    }

    return (
        <main className="create-runbook">
            <header className="create-runbook__header">
                <Link
                    className="create-runbook__back"
                    to="/runbooks"
                >
                    ← Back to Runbooks
                </Link>

                <h1>Create Runbook</h1>

                <p>
                    Add operational guidance that engineers can use during
                    incident investigation.
                </p>
            </header>

            <section className="create-runbook__form">
                {/*
                 * RunbookForm owns the form fields and local input state.
                 *
                 * This page supplies the submission behaviour and passes
                 * API state/errors down as props.
                 */}
                <RunbookForm
                    submitLabel="Create Runbook"
                    isSubmitting={isSubmitting}
                    fieldErrors={fieldErrors}
                    submitError={submitError}
                    onCancel={() => navigate("/runbooks")}
                    onSubmit={(request) =>
                        /*
                         * RunbookForm supports both create and update requests,
                         * so its onSubmit parameter is typed as a union.
                         *
                         * This page only creates runbooks, so we tell TypeScript
                         * to treat the submitted values as CreateRunbookRequest.
                         */
                        handleSubmit(request as CreateRunbookRequest)
                    }
                />
            </section>
        </main>
    );
}