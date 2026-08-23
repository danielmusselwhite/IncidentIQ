/** The severity of an incident. */
export type IncidentSeverity =
    | "Low"
    | "Medium"
    | "High"
    | "Critical";

/** The status of an incident. */
export type IncidentStatus =
    | "Queued"
    | "Processing"
    | "Completed"
    | "Failed";

/** The data required to create a new incident. */
export interface CreateIncidentRequest {
    title: string;
    description: string;
    service: string;
    environment: string;
    severity: IncidentSeverity;
    symptoms?: string;
}

/** Represents an incident and its current processing state. */
export interface Incident {
    id: string;
    title: string;
    description: string;
    service: string;
    environment: string;
    severity: IncidentSeverity;
    symptoms?: string;
    status: IncidentStatus;
    createdAt: string;
    updatedAt: string;
}