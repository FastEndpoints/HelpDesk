import type { ProblemDetails } from './errors';

/** Maps FastEndpoints problem-details field errors into a field → messages record. */
export function mapProblemFieldErrors(problem: ProblemDetails): Record<string, string[]> {
	const errors = problem.errors;
	if (!errors) return {};

	if (Array.isArray(errors)) {
		const mapped: Record<string, string[]> = {};
		for (const item of errors) {
			if (!isObject(item)) continue;
			const name = typeof item.name === 'string' ? normalizeFieldName(item.name) : '';
			const reason = typeof item.reason === 'string' ? item.reason : undefined;
			if (!name || !reason) continue;
			(mapped[name] ??= []).push(reason);
		}
		return mapped;
	}

	if (isObject(errors)) {
		const mapped: Record<string, string[]> = {};
		for (const [name, value] of Object.entries(errors)) {
			const key = normalizeFieldName(name);
			if (Array.isArray(value)) {
				mapped[key] = value.filter((entry): entry is string => typeof entry === 'string');
			} else if (typeof value === 'string') {
				mapped[key] = [value];
			}
		}
		return mapped;
	}

	return {};
}

function normalizeFieldName(name: string): string {
	const trimmed = name.trim();
	if (!trimmed) return trimmed;
	return trimmed.charAt(0).toLowerCase() + trimmed.slice(1);
}

function isObject(value: unknown): value is Record<string, unknown> {
	return typeof value === 'object' && value !== null && !Array.isArray(value);
}
