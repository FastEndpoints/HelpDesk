import { isActionFailure } from '@sveltejs/kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import { createIdentityApi } from '$lib/server/api/identity';
import { actions, type RegisterFormState } from './+page.server';

vi.mock('$lib/server/api/identity', () => ({
	createIdentityApi: vi.fn()
}));

const createIdentityApiMock = vi.mocked(createIdentityApi);
const post = vi.fn();

const VALID = {
	email: 'user@example.test',
	password: 'long-enough-password',
	confirmPassword: 'long-enough-password'
};

function requestOf(fields: Record<string, string>): Request {
	const body = new FormData();
	for (const [key, value] of Object.entries(fields)) {
		body.set(key, value);
	}
	return new Request('http://frontend.test/register', { method: 'POST', body });
}

async function submit(fields: Record<string, string>) {
	const action = actions.register;
	if (typeof action !== 'function') throw new Error('register action missing');
	return action({ request: requestOf(fields) } as never);
}

function expectFailure(result: unknown): { status: number; data: RegisterFormState } {
	expect(isActionFailure(result)).toBe(true);
	if (!isActionFailure(result) || result.data === undefined) {
		throw new Error('expected ActionFailure with data');
	}
	return { status: result.status, data: result.data as RegisterFormState };
}

describe('register form action', () => {
	beforeEach(() => {
		post.mockReset();
		createIdentityApiMock.mockReset();
		createIdentityApiMock.mockReturnValue({ POST: post } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('rejects missing fields before calling Identity', async () => {
		const result = expectFailure(await submit({ email: '', password: '', confirmPassword: '' }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: '' },
			errors: {
				email: ['Email is required.'],
				password: ['Password is required.'],
				confirmPassword: ['Confirm your password.']
			}
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('rejects invalid email, short password, and mismatch', async () => {
		const result = expectFailure(
			await submit({
				email: 'not-an-email',
				password: 'short',
				confirmPassword: 'different'
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			email: ['Email must be a valid email address.'],
			password: ['Password must be at least 12 characters.'],
			confirmPassword: ['Passwords do not match.']
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('rejects oversized email and password', async () => {
		const result = expectFailure(
			await submit({
				email: `${'a'.repeat(310)}@example.test`,
				password: 'x'.repeat(129),
				confirmPassword: 'x'.repeat(129)
			})
		);

		expect(result.data.errors).toEqual({
			email: ['Email must be at most 320 characters.'],
			password: ['Password must be at most 128 characters.']
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('trims email and posts only email + password to Identity', async () => {
		post.mockResolvedValue({
			data: 'Signup successful. Please check your email for a verification link.',
			error: undefined,
			response: new Response()
		});

		const result = await submit({
			email: '  user@example.test  ',
			password: VALID.password,
			confirmPassword: VALID.confirmPassword
		});

		expect(isActionFailure(result)).toBe(false);
		expect(result).toMatchObject({
			success: true,
			message: 'Signup successful. Please check your email for a verification link.',
			values: { email: 'user@example.test' },
			errors: {}
		});
		expect(post).toHaveBeenCalledWith('/identities/register', {
			body: { email: 'user@example.test', password: VALID.password }
		});
	});

	it('falls back to the default success message when Identity returns an empty body', async () => {
		post.mockResolvedValue({ data: '', error: undefined, response: new Response() });

		const result = await submit(VALID);

		expect(result).toMatchObject({
			success: true,
			message: 'Signup successful. Please check your email for a verification link.'
		});
	});

	it('maps Identity field errors onto the form', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more validation errors occurred.',
				errors: [{ name: 'Email', reason: 'Email address is in use!' }]
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: VALID.email },
			errors: {
				email: ['Email address is in use!']
			}
		});
	});

	it('maps Identity password field errors onto the form', async () => {
		post.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				errors: [
					{ name: 'Password', reason: 'The length of Password must be at least 12 characters.' }
				]
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.data.errors).toEqual({
			password: ['The length of Password must be at least 12 characters.']
		});
	});

	it('surfaces non-field ApiError details as a form error', async () => {
		post.mockRejectedValue(
			new ApiError(503, {
				status: 503,
				title: 'Service Unavailable',
				detail: 'Identity is temporarily offline.'
			})
		);

		const result = expectFailure(await submit(VALID));

		expect(result.status).toBe(503);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: VALID.email },
			message: 'Identity is temporarily offline.',
			errors: {
				form: ['Identity is temporarily offline.']
			}
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
	});
});

async function submitResend(fields: Record<string, string>) {
	const action = actions.resend;
	if (typeof action !== 'function') throw new Error('resend action missing');
	return action({ request: requestOf(fields) } as never);
}

describe('register resend action', () => {
	beforeEach(() => {
		post.mockReset();
		createIdentityApiMock.mockReset();
		createIdentityApiMock.mockReturnValue({ POST: post } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('rejects invalid email before calling Identity', async () => {
		const result = expectFailure(await submitResend({ email: 'not-an-email' }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: true,
			values: { email: 'not-an-email' },
			errors: { resend: ['Email must be a valid email address.'] }
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('rejects empty and overlong emails before calling Identity', async () => {
		const empty = expectFailure(await submitResend({ email: '' }));
		expect(empty.status).toBe(400);
		expect(empty.data.errors).toEqual({ resend: ['Email is required.'] });
		expect(empty.data.success).toBe(true);

		const longEmail = `${'a'.repeat(310)}@example.com`;
		const overlong = expectFailure(await submitResend({ email: longEmail }));
		expect(overlong.status).toBe(400);
		expect(overlong.data.errors).toEqual({
			resend: ['Email must be at most 320 characters.']
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('posts trimmed email and keeps the check-email success state', async () => {
		post.mockResolvedValue({
			data: 'If an account needs verification, we sent a link.',
			error: undefined,
			response: new Response()
		});

		const result = await submitResend({ email: '  user@example.test  ' });

		expect(isActionFailure(result)).toBe(false);
		expect(result).toMatchObject({
			success: true,
			resendSuccess: true,
			message: 'If an account needs verification, we sent a link.',
			values: { email: 'user@example.test' },
			errors: {}
		});
		expect(post).toHaveBeenCalledWith('/identities/resend-verification', {
			body: { email: 'user@example.test' }
		});
	});

	it('falls back to the default resend message when Identity returns an empty body', async () => {
		post.mockResolvedValue({ data: '', error: undefined, response: new Response() });

		const result = await submitResend({ email: VALID.email });

		expect(result).toMatchObject({
			success: true,
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
			success: true,
			values: { email: VALID.email },
			errors: { resend: ['Email is required.'] }
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
			success: true,
			errors: { resend: ['Identity is busy.'] }
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
		expect(result.data.errors).toEqual({ resend: ['Weird status'] });
	});

	it('uses a safe resend error when Identity is unreachable', async () => {
		post.mockRejectedValue(new Error('ECONNREFUSED'));

		const result = expectFailure(await submitResend({ email: VALID.email }));

		expect(result.status).toBe(500);
		expect(result.data.errors).toEqual({
			resend: ['Unable to reach the identity service. Please try again later.']
		});
		expect(result.data.success).toBe(true);
	});
});
