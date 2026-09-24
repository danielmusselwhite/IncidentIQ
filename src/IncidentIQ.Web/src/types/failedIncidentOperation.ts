import type { IncidentSeverity } from "./incident";

export interface FailedIncidentOperation {
    id: string;
    title: string;
    service: string;
    environment: string;
    severity: IncidentSeverity;
    failureReason: string | null;
    attemptCount: number;
    createdAt: string;
    processingStartedAt: string | null;
    lastAttemptAt: string | null;
    failedAt: string | null;
}