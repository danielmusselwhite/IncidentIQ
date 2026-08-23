import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";

import { ApiError } from "../../api/apiError";
import { createIncident } from "../../api/incidentsApi";
import type {
    CreateIncidentRequest,
    IncidentSeverity,
} from "../../types/incident";

import "./SubmitIncidentPage.css";

const initialForm: CreateIncidentRequest = {
    title: "",
    description: "",
    service: "",
    environment: "",
    severity: "Medium",
    symptoms: "",
};

export default function SubmitIncidentPage() {
    const navigate = useNavigate();

    const [form, setForm] = useState<CreateIncidentRequest>(initialForm);
    const [errors, setErrors] = useState<Record<string, string[]>>({});
    const [submitError, setSubmitError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async (event: FormEvent) => {
        event.preventDefault();

        setErrors({});
        setSubmitError(null);
        setIsSubmitting(true);

        try {
            const incident = await createIncident(form);
            navigate(`/incidents/${incident.id}`);
        } catch (error) {
            if (error instanceof ApiError) {
                setErrors(error.errors);
                setSubmitError(error.message);
            } else {
                setSubmitError("Unable to create incident.");
            }
        } finally {
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
                                setForm({
                                    ...form,
                                    title: event.target.value,
                                })
                            }
                            placeholder="e.g. Payments API timing out"
                        />

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

                {submitError && Object.keys(errors).length === 0 && (
                    <div className="incident-form__error">
                        {submitError}
                    </div>
                )}

                <div className="incident-form__actions">
                    <button
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
                        {isSubmitting ? "Submitting..." : "Submit Incident"}
                    </button>
                </div>
            </form>
        </main>
    );
}

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