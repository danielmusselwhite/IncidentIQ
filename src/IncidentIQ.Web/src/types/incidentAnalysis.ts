/**
 * Represents the analysis of an incident, including likely causes and recommended actions.
 * Aligns with the IncidentIQ IncidentAnalysisResponse
 */
export interface IncidentAnalysis {
    summary: string;
    likelyCauses: LikelyCause[];
    recommendedActions: RecommendedAction[];
    evidence: IncidentAnalysisEvidence;
    model: string;
    analysedAtUtc: string;
}

/**
 * Represents a likely cause of an incident, including the confidence level.
 */
export interface LikelyCause {
    cause: string;
    confidence: number;
    evidenceReferences: string[];
}

/**
 * Represents a recommended action for an incident.
 */
export interface RecommendedAction {
    action: string;
    evidenceReferences: string[];
}

/**
 * Represents the evidence associated with an incident analysis.
 */
export interface IncidentAnalysisEvidence {
    historicalIncidents: HistoricalIncidentEvidence[];
    runbookChunks: RunbookChunkEvidence[];
}

/**
 * Represents the evidence of a historical incident that is related to the current incident analysis.
 */
export interface HistoricalIncidentEvidence {
    referenceId: string;
    incidentId: string;
    title: string;
    description: string;
    symptoms: string | null;
    service: string;
    environment: string;
    severity: string;
    completedAtUtc: string;
    distance: number;
}

export interface RunbookChunkEvidence {
    referenceId: string;
    runbookId: string;
    chunkIndex: number;
    title: string;
    service: string;
    content: string;
    distance: number;
}