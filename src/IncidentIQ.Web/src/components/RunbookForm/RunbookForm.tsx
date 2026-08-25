import { useState, type FormEvent } from "react";

import type {
    CreateRunbookRequest,
    UpdateRunbookRequest,
} from "../../types/runbook";

import "./RunbookForm.css";

/**
 * Represents the values stored and edited by the runbook form.
 */
interface RunbookFormValues {
    title: string;
    description: string;
    service: string;
    content: string;
}

/**
 * Defines the values and behaviour that a parent component must provide
 * when using RunbookForm.
 */
interface RunbookFormProps {
    // Optional values used to pre-populate the form, such as when editing.
    initialValues?: RunbookFormValues;

    // Text displayed on the submit button, e.g. "Create Runbook" or "Save Changes".
    submitLabel: string;

    // Used to disable the form while a request is in progress.
    isSubmitting: boolean;

    // Validation errors returned by the API, grouped by field name.
    fieldErrors?: Record<string, string[]>;

    // General submission error not associated with a particular field.
    submitError?: string | null;

    // Optional action used by the parent to return from the form.
    onCancel?: () => void;

    // Function provided by the parent component that performs the actual
    // create or update API request.
    onSubmit: (
        request: CreateRunbookRequest | UpdateRunbookRequest,
    ) => Promise<void>;
}

/**
 * Default values used when creating a new runbook.
 */
const emptyValues: RunbookFormValues = {
    title: "",
    description: "",
    service: "",
    content: "",
};

/**
 * Reusable form for creating or editing a runbook.
 *
 * The component manages the form field values itself, while the parent
 * component is responsible for what happens when the form is submitted.
 *
 * This allows the same form UI to be reused for both creating and updating
 * runbooks without duplicating the form markup.
 */
export default function RunbookForm({
    initialValues = emptyValues,
    submitLabel,
    isSubmitting,
    fieldErrors = {},
    submitError,
    onCancel,
    onSubmit,
}: RunbookFormProps) {
    // Initialise the form state using either the supplied values (editing)
    // or the empty defaults (creating).
    const [values, setValues] = useState<RunbookFormValues>(initialValues);

    /**
     * Updates a single field within the form state.
     *
     * @param field - Name of the field to update.
     * @param value - New value entered by the user.
     */
    function updateField(field: keyof RunbookFormValues, value: string) {
        /*
         * The callback form of setValues receives the latest state value.
         *
         * ...current copies the existing form values, while [field] uses
         * the supplied field name dynamically to replace just that property.
         */
        setValues((current) => ({
            ...current,
            [field]: value,
        }));
    }

    /**
     * Handles the form submission.
     *
     * The form itself does not know whether it is creating or updating a
     * runbook. Instead, it passes its current values to the onSubmit function
     * supplied by the parent component.
     *
     * @param event - The React form submission event.
     */
    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        // Prevent the browser's default behaviour of refreshing the page.
        event.preventDefault();

        await onSubmit(values);
    }

    const titleErrors = fieldErrors.Title ?? fieldErrors.title;
    const serviceErrors = fieldErrors.Service ?? fieldErrors.service;
    const descriptionErrors =
        fieldErrors.Description ?? fieldErrors.description;
    const contentErrors = fieldErrors.Content ?? fieldErrors.content;

    return (
        <form
            className="runbook-form"
            onSubmit={handleSubmit}
        >
            {submitError && (
                <div className="runbook-form__error">
                    {submitError}
                </div>
            )}

            <div className="runbook-form__section">
                <h2>Runbook Details</h2>

                <div className="form-field">
                    <label htmlFor="title">Title</label>

                    <input
                        id="title"
                        value={values.title}
                        onChange={(event) =>
                            updateField("title", event.target.value)
                        }
                        placeholder="e.g. Payments API timeout recovery"
                        disabled={isSubmitting}
                        aria-invalid={Boolean(titleErrors?.length)}
                    />

                    <FieldErrors errors={titleErrors} />
                </div>

                <div className="form-field">
                    <label htmlFor="service">Service</label>

                    <input
                        id="service"
                        value={values.service}
                        onChange={(event) =>
                            updateField("service", event.target.value)
                        }
                        placeholder="e.g. Payments"
                        disabled={isSubmitting}
                        aria-invalid={Boolean(serviceErrors?.length)}
                    />

                    <FieldErrors errors={serviceErrors} />
                </div>

                <div className="form-field">
                    <label htmlFor="description">Description</label>

                    <textarea
                        id="description"
                        rows={4}
                        value={values.description}
                        onChange={(event) =>
                            updateField("description", event.target.value)
                        }
                        placeholder="Briefly describe when and why this runbook should be used..."
                        disabled={isSubmitting}
                        aria-invalid={Boolean(descriptionErrors?.length)}
                    />

                    <FieldErrors errors={descriptionErrors} />
                </div>
            </div>

            <div className="runbook-form__section">
                <h2>Operational Guidance</h2>

                <div className="form-field">
                    <label htmlFor="content">Runbook Content</label>

                    <textarea
                        id="content"
                        className="runbook-form__content"
                        rows={16}
                        value={values.content}
                        onChange={(event) =>
                            updateField("content", event.target.value)
                        }
                        placeholder="Add investigation steps, checks, remediation guidance and useful operational notes..."
                        disabled={isSubmitting}
                        aria-invalid={Boolean(contentErrors?.length)}
                    />

                    <FieldErrors errors={contentErrors} />
                </div>
            </div>

            <div className="runbook-form__actions">
                {onCancel && (
                    <button
                        type="button"
                        className="button button--secondary"
                        onClick={onCancel}
                        disabled={isSubmitting}
                    >
                        Cancel
                    </button>
                )}

                <button
                    type="submit"
                    className="button button--primary"
                    disabled={isSubmitting}
                >
                    {isSubmitting ? "Saving..." : submitLabel}
                </button>
            </div>
        </form>
    );
}

/**
 * Displays validation messages associated with a single form field.
 *
 * Returns null when there are no errors, meaning React renders nothing.
 *
 * @param errors - Optional list of validation messages for the field.
 */
function FieldErrors({ errors }: { errors?: string[] }) {
    if (!errors?.length) {
        return null;
    }

    return (
        <div className="form-field__errors">
            {errors.map((error) => (
                <span key={error}>{error}</span>
            ))}
        </div>
    );
}