/**
 * Represents the standard error response returned by the API.
 *
 * All properties are optional because different API failures may return
 * different amounts of information.
 */
export interface ApiProblemDetails {
    title?: string;
    status?: number;
    detail?: string;
    errors?: Record<string, string[]>;
}

/**
 * Custom Error type used to represent errors returned by the backend API.
 *
 * In addition to the standard JavaScript error message, it stores the HTTP
 * status code and any field-level validation errors returned by the API.
 */
export class ApiError extends Error {
    public status: number;
    public errors: Record<string, string[]>;

    constructor(
        message: string,
        status: number,
        errors: Record<string, string[]> = {},
    ) {
        super(message);
        this.status = status;
        this.errors = errors;
    }
}