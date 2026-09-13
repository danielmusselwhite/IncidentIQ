export type AssistantConversationRole =
    | "user"
    | "assistant";

export interface AssistantConversationTurn {
    role: AssistantConversationRole;
    content: string;
}

export interface AskOperationalQuestionRequest {
    question: string;
    service: string | null;
    environment: string | null;
    conversationHistory: AssistantConversationTurn[];
}

export interface OperationalAssistantResponse {
    sections: OperationalAnswerSection[];
    evidence: AssistantEvidence;
    model: string;
    answeredAtUtc: string;
}

export interface OperationalAnswerSection {
    content: string;
    evidenceReferences: string[];
}

export interface AssistantEvidence {
    historicalIncidents: AssistantHistoricalIncidentEvidence[];
    runbookChunks: AssistantRunbookEvidence[];
}

export interface AssistantHistoricalIncidentEvidence {
    referenceId: string;
    incidentId: string;
    title: string;
    description: string;
    symptoms: string | null;
    service: string;
    environment: string;
    severity: string;
    completedAtUtc: string;
}

export interface AssistantRunbookEvidence {
    referenceId: string;
    runbookId: string;
    chunkIndex: number;
    title: string;
    service: string;
    content: string;
}