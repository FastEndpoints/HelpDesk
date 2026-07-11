import type { paths } from '$lib/api/generated/identity';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createServerClient } from './client';
import { ApiError } from './errors';

describe('server API client', () => {
	afterEach(() => vi.unstubAllGlobals());

	it('throws an ApiError with backend problem details for non-success responses', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn().mockResolvedValue(
				Response.json(
					{
						type: 'https://example.test/problems/conflict',
						title: 'Conflict',
						detail: 'The identity already exists.',
						errors: { email: ['Email is already registered.'] },
						traceId: 'test-trace'
					},
					{ status: 409 }
				)
			)
		);

		const request = createServerClient<paths>('http://identity.test').POST('/identities/login', {
			body: { email: 'user@example.test', password: 'password' }
		});

		await expect(request).rejects.toMatchObject({
			name: 'ApiError',
			status: 409,
			problem: {
				status: 409,
				detail: 'The identity already exists.',
				errors: { email: ['Email is already registered.'] },
				traceId: 'test-trace'
			}
		} satisfies Partial<ApiError>);
	});
});
