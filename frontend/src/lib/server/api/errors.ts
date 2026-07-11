export interface ProblemDetails {
	type?: string;
	title?: string;
	status: number;
	detail?: string;
	instance?: string;
	errors?: Record<string, string[]>;
	[key: string]: unknown;
}

export class ApiError extends Error {
	constructor(
		public readonly status: number,
		public readonly problem: ProblemDetails
	) {
		super(problem.detail ?? problem.title ?? `Backend request failed (${status})`);
		this.name = 'ApiError';
	}
}

export async function toApiError(response: Response): Promise<ApiError> {
	let body: unknown;
	try {
		body = await response.json();
	} catch {
		body = undefined;
	}

	const problem = isObject(body)
		? ({ ...body, status: response.status } as ProblemDetails)
		: { status: response.status, title: response.statusText || 'Backend request failed' };
	return new ApiError(response.status, problem);
}

function isObject(value: unknown): value is Record<string, unknown> {
	return typeof value === 'object' && value !== null && !Array.isArray(value);
}
