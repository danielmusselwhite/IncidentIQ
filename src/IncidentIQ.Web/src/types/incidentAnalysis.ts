/**
 * Represents the analysis of an incident, including likely causes and recommended actions.
 * Aligns with the IncidentIQ IncidentAnalysisResponse
 */
export interface IncidentAnalysis {
    summary: string;
    likelyCauses: LikelyCause[];
    recommendedActions: RecommendedAction[];
    model: string;
    analysedAtUtc: string;
}

/**
 * Represents a likely cause of an incident, including the confidence level.
 */
export interface LikelyCause {
    cause: string;
    confidence: number;
}

/**
 * Represents a recommended action for an incident.
 */
export interface RecommendedAction {
    action: string;
}