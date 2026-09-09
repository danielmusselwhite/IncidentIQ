/** The Runbook entity represents a runbook document in the system. */
export interface Runbook {
  id: string;
  title: string;
  description: string;
  service: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

/** The request payload for creating a new runbook. */
export interface CreateRunbookRequest {
  title: string;
  description: string;
  service: string;
  content: string;
}

/** The request payload for updating an existing runbook. */
export interface UpdateRunbookRequest {
  title: string;
  description: string;
  service: string;
  content: string;
}

/**
 * Represents a Runbook chunk returned by semantic vector search.
 * Lower distance values indicate a closer semantic match.
 */
export interface RunbookChunkMatch {
    runbookId: string;
    chunkIndex: number;
    title: string;
    service: string;
    content: string;
    distance: number;
}