import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { createIncident } from "../../api/incidentsApi";
import type {
    CreateIncidentRequest,
    IncidentSeverity,
} from "../../types/incident";

import "./SubmitIncidentPage.css";

/**
 * Default values used when the incident form is first displayed.
 *
 * Keeping the initial state outside the component avoids recreating this
 * object every time the component renders.
 */
const initialForm: CreateIncidentRequest = {
    title: "",
    description: "",
    service: "",
    environment: "",
    severity: "Medium",
    symptoms: "",
};

/**
 * Displays the form used to create a new incident.
 *
 * The component stores the user's form values in React state, submits them
 * to the API, displays any validation errors, and redirects to the newly
 * created incident when submission succeeds.
 */
export default function SubmitIncidentPage() {
    // useNavigate allows navigation to another route from JavaScript,
    // rather than requiring the user to click a <Link>.
    const navigate = useNavigate();

    // Stores all form field values as a single object.
    const [form, setForm] = useState<CreateIncidentRequest>(initialForm);

    // Stores validation errors returned by the API.
    // Each field can have one or more associated error messages.
    const [errors, setErrors] = useState<Record<string, string[]>>({});

    // Stores an error relating to the overall submission rather than
    // a specific form field.
    const [submitError, setSubmitError] = useState<string | null>(null);

    // Used to disable the form buttons and display submission progress.
    const [isSubmitting, setIsSubmitting] = useState(false);

    /**
     * Handles form submission.
     *
     * Prevents the browser's normal form submission, sends the form data
     * through the API, and navigates to the new incident if successful.
     *
     * @param event - The React form submission event.
     */
    const handleSubmit = async (event: FormEvent) => {
        // Prevent the browser from refreshing the page when the form submits.
        event.preventDefault();

        // Clear errors from any previous submission attempt.
        setErrors({});
        setSubmitError(null);
        setIsSubmitting(true);

        try {
            // createIncident sends a POST request to the API with the form data.
            const incident = await createIncident(form);

            // Navigate directly to the detail page for the newly created incident.
            navigate(`/incidents/${incident.id}`);
        } catch (error) {
            if (error instanceof ApiError) {
                // ApiError can contain validation errors associated with
                // individual fields, such as Title or Description.
                setErrors(error.errors);
                setSubmitError(error.message);
            } else {
                // Handle unexpected failures that do not originate from
                // the normal API error response format.
                setSubmitError("Unable to create incident.");
            }
        } finally {
            // finally runs regardless of whether submission succeeds or fails.
            setIsSubmitting(false);
        }
    };

    return (
        <main className="submit-incident">
            <div className="submit-incident__header">
                <h1>Submit Incident</h1>
                <p>
                    Record an operational incident for analysis and investigation.
                </p>
            </div>

            <form
                className="incident-form"
                onSubmit={handleSubmit}
            >
                <div className="incident-form__section">
                    <h2>Incident Details</h2>

                    <div className="form-field form-field--full">
                        <label htmlFor="title">Title</label>

                        <input
                            id="title"
                            value={form.title}
                            onChange={(event) =>
                                // React state should not be modified directly.
                                // Spread (...) copies the existing form before
                                // replacing only the title value.
                                setForm({
                                    ...form,
                                    title: event.target.value,
                                })
                            }
                            placeholder="e.g. Payments API timing out"
                        />

                        {/* Display any API validation errors for this field. */}
                        <FieldErrors errors={errors.Title} />
                    </div>

                    <div className="incident-form__grid">
                        <div className="form-field">
                            <label htmlFor="service">Service</label>

                            <input
                                id="service"
                                value={form.service}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        service: event.target.value,
                                    })
                                }
                                placeholder="e.g. Payments"
                            />

                            <FieldErrors errors={errors.Service} />
                        </div>

                        <div className="form-field">
                            <label htmlFor="environment">Environment</label>

                            <input
                                id="environment"
                                value={form.environment}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        environment: event.target.value,
                                    })
                                }
                                placeholder="e.g. Production"
                            />

                            <FieldErrors errors={errors.Environment} />
                        </div>
                    </div>

                    <div className="form-field">
                        <label htmlFor="severity">Severity</label>

                        <select
                            id="severity"
                            value={form.severity}
                            onChange={(event) =>
                                setForm({
                                    ...form,

                                    // HTML select values are always returned as
                                    // strings, so tell TypeScript this value is
                                    // one of the allowed IncidentSeverity values.
                                    severity: event.target.value as IncidentSeverity,
                                })
                            }
                        >
                            <option value="Low">Low</option>
                            <option value="Medium">Medium</option>
                            <option value="High">High</option>
                            <option value="Critical">Critical</option>
                        </select>

                        <FieldErrors errors={errors.Severity} />
                    </div>
                </div>

                <div className="incident-form__section">
                    <h2>Incident Information</h2>

                    <div className="form-field">
                        <label htmlFor="description">Description</label>

                        <textarea
                            id="description"
                            rows={6}
                            value={form.description}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    description: event.target.value,
                                })
                            }
                            placeholder="Describe what happened and the impact..."
                        />

                        <FieldErrors errors={errors.Description} />
                    </div>

                    <div className="form-field">
                        <label htmlFor="symptoms">Symptoms</label>

                        <textarea
                            id="symptoms"
                            rows={4}
                            value={form.symptoms}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    symptoms: event.target.value,
                                })
                            }
                            placeholder="Errors, unusual behaviour, alerts or other symptoms..."
                        />

                        <FieldErrors errors={errors.Symptoms} />
                    </div>
                </div>

                {/*
                 * Show the general submission error only when there are no
                 * field-specific validation errors already being displayed.
                 *
                 * && is commonly used in JSX for conditional rendering:
                 * when the condition is false, nothing is rendered.
                 */}
                {submitError && Object.keys(errors).length === 0 && (
                    <div className="incident-form__error">
                        {submitError}
                    </div>
                )}

                <div className="incident-form__actions">
                    <button
                        // type="button" prevents this button from submitting
                        // the surrounding form.
                        type="button"
                        className="button button--secondary"
                        onClick={() => navigate("/incidents")}
                        disabled={isSubmitting}
                    >
                        Cancel
                    </button>

                    <button
                        type="submit"
                        className="button button--primary"
                        disabled={isSubmitting}
                    >
                        {/*
                         * Change the button text while the request is running
                         * to give the user feedback that submission is in progress.
                         */}
                        {isSubmitting ? "Submitting..." : "Submit Incident"}
                    </button>
                </div>
            </form>
        </main>
    );
}

/**
 * Displays validation errors for a single form field.
 *
 * If there are no errors, the component returns null so React renders
 * nothing for this section.
 *
 * @param errors - Optional list of validation messages for the field.
 */
function FieldErrors({ errors }: { errors?: string[] }) {
    // Optional chaining (?.) safely handles errors being undefined.
    if (!errors?.length) {
        return null;
    }

    return (
        <div className="form-field__errors">
            {/*
             * map converts each error string into a React element.
             *
             * React requires a key when rendering lists so it can efficiently
             * identify which elements have changed between renders.
             */}
            {errors.map((error) => (
                <span key={error}>{error}</span>
            ))}
        </div>
    );
}