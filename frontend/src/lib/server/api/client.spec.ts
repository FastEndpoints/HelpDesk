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

	it('sends Authorization bearer when a token is provided', async () => {
		const fetchMock = vi.fn().mockResolvedValue(
			Response.json(
				{
					id: 'id-1',
					email: 'user@example.test',
					accessToken: 'jwt',
					expiresAt: '2026-01-08T00:00:00.000Z'
				},
				{ status: 200 }
			)
		);
		vi.stubGlobal('fetch', fetchMock);

		await createServerClient<paths>('http://identity.test', 'server-held-jwt').POST(
			'/identities/login',
			{
				body: { email: 'user@example.test', password: 'password' }
			}
		);

		expect(fetchMock).toHaveBeenCalledOnce();
		const request = fetchMock.mock.calls[0]?.[0] as Request;
		expect(request.headers.get('Authorization')).toBe('Bearer server-held-jwt');
	});

	it('omits Authorization when no token is provided', async () => {
		const fetchMock = vi.fn().mockResolvedValue(
			Response.json(
				{
					id: 'id-1',
					email: 'user@example.test',
					accessToken: 'jwt',
					expiresAt: '2026-01-08T00:00:00.000Z'
				},
				{ status: 200 }
			)
		);
		vi.stubGlobal('fetch', fetchMock);

		await createServerClient<paths>('http://identity.test').POST('/identities/login', {
			body: { email: 'user@example.test', password: 'password' }
		});

		const request = fetchMock.mock.calls[0]?.[0] as Request;
		expect(request.headers.get('Authorization')).toBeNull();
	});
});
