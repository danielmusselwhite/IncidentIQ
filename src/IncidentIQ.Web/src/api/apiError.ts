export interface ApiProblemDetails {
    title?: string;
    status?: number;
    detail?: string;
    errors?: Record<string, string[]>;
}

export class ApiError extends Error {
    constructor(
        message: string,
        public status: number,
        public errors: Record<string, string[]> = {},
    ) {
        super(message);
    }
}