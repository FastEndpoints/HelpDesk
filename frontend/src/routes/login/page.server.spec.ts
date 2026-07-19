import { isActionFailure, isRedirect } from '@sveltejs/kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import { createIdentityApi } from '$lib/server/api/identity';
import { writeSessionToken } from '$lib/server/api/session';
import { actions, load, type LoginFormState } from './+page.server';

vi.mock('$lib/server/api/identity', () => ({
	createIdentityApi: vi.fn()
}));

vi.mock('$lib/server/api/session', () => ({
	writeSessionToken: vi.fn()
}));

const createIdentityApiMock = vi.mocked(createIdentityApi);
const writeSessionTokenMock = vi.mocked(writeSessionToken);
const post = vi.fn();
const cookies = {
	get: vi.fn(),
	set: vi.fn(),
	delete: vi.fn()
};

const VALID = {
	email: 'user@example.test',
	password: 'long-enough-password'
};

function requestOf(fields: Record<string, string>): Request {
	const body = new FormData();
	for (const [key, value] of Object.entries(fields)) {
		body.set(key, value);
	}
	return new Request('http://frontend.test/login', { method: 'POST', body });
}

async function submit(fields: Record<string, string>) {
	const action = actions.login;
	if (typeof action !== 'function') throw new Error('login action missing');
	return action({ request: requestOf(fields), cookies } as never);
}

async function loadWith(search = '') {
	return load({
		url: new URL(`http://frontend.test/login${search}`)
	} as never);
}

function expectFailure(result: unknown): { status: number; data: LoginFormState } {
	expect(isActionFailure(result)).toBe(true);
	if (!isActionFailure(result) || result.data === undefined) {
		throw new Error('expected ActionFailure with data');
	}
	return { status: result.status, data: result.data as LoginFormState };
}

describe('login form action', () => {
	beforeEach(() => {
		post.mockReset();
		createIdentityApiMock.mockReset();
		writeSessionTokenMock.mockReset();
		cookies.get.mockReset();
		cookies.set.mockReset();
		cookies.delete.mockReset();
		createIdentityApiMock.mockReturnValue({ POST: post } as never);
		vi.useFakeTimers();
		vi.setSystemTime(new Date('2026-01-01T00:00:00.000Z'));
	});

	afterEach(() => {
		vi.useRealTimers();
		vi.clearAllMocks();
	});

	it('rejects missing fields before calling Identity', async () => {
		const result = expectFailure(await submit({ email: '', password: '' }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: '' },
			errors: {
				email: ['Email is required.'],
				password: ['Password is required.']
			}
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('rejects invalid email and oversized password', async () => {
		const result = expectFailure(
			await submit({
				email: 'not-an-email',
				password: 'x'.repeat(129)
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			email: ['Email must be a valid email address.'],
			password: ['Password must be at most 128 characters.']
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('rejects oversized email before calling Identity', async () => {
		const result = expectFailure(
			await submit({
				email: `${'a'.repeat(310)}@example.test`,
				password: VALID.password
			})
		);

		expect(result.data.errors).toEqual({
			email: ['Email must be at most 320 characters.']
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('defaults redirectTo to home and accepts a safe relative path', async () => {
		expect(await loadWith()).toEqual({ redirectTo: '/' });
		expect(await loadWith('?redirectTo=%2Fsettings%2Fprofile')).toEqual({
			redirectTo: '/settings/profile'
		});
		expect(await loadWith('?redirectTo=https://evil.example')).toEqual({ redirectTo: '/' });
		expect(await loadWith('?redirectTo=//evil.example')).toEqual({ redirectTo: '/' });
	});

	it('trims email, posts credentials, sets session cookie from expiresAt, and redirects home', async () => {
		const expiresAt = '2026-01-08T00:00:00.000Z';
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token',
				expiresAt
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit({
				email: '  user@example.test  ',
				password: VALID.password
			});
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
			if (!isRedirect(error)) throw error;
			expect(error.status).toBe(303);
			expect(error.location).toBe('/');
		}

		expect(post).toHaveBeenCalledWith('/identities/login', {
			body: { email: 'user@example.test', password: VALID.password }
		});
		expect(writeSessionTokenMock).toHaveBeenCalledWith(
			cookies,
			'jwt-access-token',
			7 * 24 * 60 * 60
		);
	});

	it('redirects to a safe redirectTo after successful login', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token',
				expiresAt: '2026-01-08T00:00:00.000Z'
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit({
				...VALID,
				redirectTo: '/settings/profile'
			});
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
			if (!isRedirect(error)) throw error;
			expect(error.status).toBe(303);
			expect(error.location).toBe('/settings/profile');
		}
	});

	it('ignores open-redirect targets after successful login', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token'
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit({
				...VALID,
				redirectTo: 'https://evil.example'
			});
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
			if (!isRedirect(error)) throw error;
			expect(error.location).toBe('/');
		}
	});

	it('uses default cookie maxAge when expiresAt is missing', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token'
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit(VALID);
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
		}

		expect(writeSessionTokenMock).toHaveBeenCalledWith(cookies, 'jwt-access-token', undefined);
	});

	it('uses default cookie maxAge when expiresAt is unparseable', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token',
				expiresAt: 'not-a-date'
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit(VALID);
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
		}

		expect(writeSessionTokenMock).toHaveBeenCalledWith(cookies, 'jwt-access-token', undefined);
	});

	it('computes maxAge from a Date expiresAt value', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token',
				expiresAt: new Date('2026-01-01T01:00:00.000Z')
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit(VALID);
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
		}

		expect(writeSessionTokenMock).toHaveBeenCalledWith(cookies, 'jwt-access-token', 3600);
	});

	it('clamps past expiresAt maxAge to zero', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				accessToken: 'jwt-access-token',
				expiresAt: '2025-12-31T00:00:00.000Z'
			},
			error: undefined,
			response: new Response()
		});

		try {
			await submit(VALID);
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
		}

		expect(writeSessionTokenMock).toHaveBeenCalledWith(cookies, 'jwt-access-token', 0);
	});

	it('fails when Identity omits the access token', async () => {
		post.mockResolvedValue({
			data: {
				id: 'user-1',
				email: 'user@example.test',
				expiresAt: '2026-01-08T00:00:00.000Z'
			},
			error: undefined,
			response: new Response()
		});

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(502);
		expect(result.data.errors).toEqual({
			form: ['Login succeeded but no access token was returned. Please try again.']
		});
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('maps Identity field errors onto the form', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more validation errors occurred.',
				errors: [{ name: 'Email', reason: 'Email is required.' }]
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: VALID.email },
			errors: {
				email: ['Email is required.']
			}
		});
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('maps Identity password field errors onto the form', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more validation errors occurred.',
				errors: [{ name: 'Password', reason: 'Password is required.' }]
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			password: ['Password is required.']
		});
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('surfaces non-field ApiError details as a form error', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more errors occurred!',
				detail: 'Invalid email or password.'
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: VALID.email },
			message: 'Invalid email or password.',
			needsVerification: false,
			errors: {
				form: ['Invalid email or password.']
			}
		});
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('marks account-not-verified failures for the resend recovery UI', async () => {
		post.mockRejectedValue(
			new ApiError(
				400,
				{
					status: 400,
					title: 'One or more errors occurred!',
					detail: 'Account not verified.'
				},
				new Headers({ 'Resend-Available-In': '1200' })
			)
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: VALID.email },
			needsVerification: true,
			resendAvailableInSeconds: 1200,
			errors: {
				form: ['Account not verified.']
			}
		});
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('omits resendAvailableInSeconds when the header is absent', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				detail: 'Account not verified.'
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.data.needsVerification).toBe(true);
		expect(result.data.resendAvailableInSeconds).toBeUndefined();
	});

	it('falls back to problem title when detail is missing', async () => {
		post.mockRejectedValue(
			new ApiError(401, {
				status: 401,
				title: 'Unauthorized'
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(401);
		expect(result.data.errors).toEqual({
			form: ['Unauthorized']
		});
		expect(result.data.message).toBe('Unauthorized');
	});

	it('falls back to a generic form error when problem has neither detail nor title', async () => {
		post.mockRejectedValue(new ApiError(400, { status: 400 }));

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			form: ['Sign-in failed. Please try again.']
		});
	});

	it('clamps out-of-range ApiError status to 400', async () => {
		post.mockRejectedValue(
			new ApiError(399, {
				status: 399,
				detail: 'Weird status'
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			form: ['Weird status']
		});
	});

	it('uses a safe form error when Identity is unreachable', async () => {
		post.mockRejectedValue(new Error('ECONNREFUSED'));

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(500);
		expect(result.data.errors).toEqual({
			form: ['Unable to reach the identity service. Please try again later.']
		});
		expect(result.data.values.email).toBe(VALID.email);
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});
});

async function submitResend(fields: Record<string, string>) {
	const action = actions.resend;
	if (typeof action !== 'function') throw new Error('resend action missing');
	return action({ request: requestOf(fields), cookies } as never);
}

describe('login resend action', () => {
	beforeEach(() => {
		post.mockReset();
		createIdentityApiMock.mockReset();
		writeSessionTokenMock.mockReset();
		createIdentityApiMock.mockReturnValue({ POST: post } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('rejects invalid email before calling Identity', async () => {
		const result = expectFailure(await submitResend({ email: 'not-an-email' }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			needsVerification: true,
			values: { email: 'not-an-email' },
			errors: {
				form: ['Account not verified.'],
				resend: ['Email must be a valid email address.']
			}
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('rejects empty and overlong emails before calling Identity', async () => {
		const empty = expectFailure(await submitResend({ email: '' }));
		expect(empty.status).toBe(400);
		expect(empty.data).toMatchObject({
			needsVerification: true,
			errors: {
				form: ['Account not verified.'],
				resend: ['Email is required.']
			}
		});

		const longEmail = `${'a'.repeat(310)}@example.com`;
		const overlong = expectFailure(await submitResend({ email: longEmail }));
		expect(overlong.status).toBe(400);
		expect(overlong.data.errors).toMatchObject({
			resend: ['Email must be at most 320 characters.']
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('posts trimmed email and keeps the not-verified recovery state', async () => {
		post.mockResolvedValue({
			data: 'If an account needs verification, we sent a link.',
			error: undefined,
			response: new Response()
		});

		const result = await submitResend({ email: '  user@example.test  ' });

		expect(isActionFailure(result)).toBe(false);
		expect(result).toMatchObject({
			success: false,
			resendSuccess: true,
			needsVerification: true,
			resendAvailableInSeconds: 1800,
			message: 'If an account needs verification, we sent a link.',
			values: { email: 'user@example.test' },
			errors: {
				form: ['Account not verified.']
			}
		});
		expect(post).toHaveBeenCalledWith('/identities/resend-verification', {
			body: { email: 'user@example.test' }
		});
		expect(writeSessionTokenMock).not.toHaveBeenCalled();
	});

	it('falls back to the default resend message when Identity returns an empty body', async () => {
		post.mockResolvedValue({ data: '', error: undefined, response: new Response() });

		const result = await submitResend({ email: VALID.email });

		expect(result).toMatchObject({
			resendSuccess: true,
			message: 'If an account needs verification, we sent a link.'
		});
	});

	it('maps Identity field errors onto the resend error', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				errors: [{ name: 'Email', reason: 'Email is required.' }]
			})
		);

		const result = expectFailure(await submitResend({ email: VALID.email }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			needsVerification: true,
			errors: {
				form: ['Account not verified.'],
				resend: ['Email is required.']
			}
		});
	});

	it('surfaces non-field ApiError details as the resend error', async () => {
		post.mockRejectedValue(
			new ApiError(503, {
				status: 503,
				detail: 'Identity is busy.'
			})
		);

		const result = expectFailure(await submitResend({ email: VALID.email }));

		expect(result.status).toBe(503);
		expect(result.data).toMatchObject({
			needsVerification: true,
			errors: {
				form: ['Account not verified.'],
				resend: ['Identity is busy.']
			}
		});
	});

	it('clamps out-of-range ApiError status to 400 on resend', async () => {
		post.mockRejectedValue(
			new ApiError(399, {
				status: 399,
				title: 'Weird status'
			})
		);

		const result = expectFailure(await submitResend({ email: VALID.email }));

		expect(result.status).toBe(400);
		expect(result.data.errors).toMatchObject({ resend: ['Weird status'] });
	});

	it('uses a safe resend error when Identity is unreachable', async () => {
		post.mockRejectedValue(new Error('ECONNREFUSED'));

		const result = expectFailure(await submitResend({ email: VALID.email }));

		expect(result.status).toBe(500);
		expect(result.data).toMatchObject({
			needsVerification: true,
			errors: {
				form: ['Account not verified.'],
				resend: ['Unable to reach the identity service. Please try again later.']
			}
		});
	});
});
