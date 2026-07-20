import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from './errors';
import { createIdentityApi } from './identity';
import {
	FORGOT_PASSWORD_SUCCESS_FALLBACK,
	postForgotPassword,
	postResetPassword,
	readResetAvailableInSeconds,
	RESET_AVAILABLE_IN_HEADER,
	RESET_PASSWORD_SUCCESS_FALLBACK,
	validateForgotEmail,
	validateResetPassword
} from './password-reset';

vi.mock('./identity', () => ({
	createIdentityApi: vi.fn()
}));

const createIdentityApiMock = vi.mocked(createIdentityApi);
const post = vi.fn();

describe('password-reset helpers', () => {
	beforeEach(() => {
		post.mockReset();
		createIdentityApiMock.mockReset();
		createIdentityApiMock.mockReturnValue({ POST: post } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('validates forgot-password email', () => {
		expect(validateForgotEmail('')).toBe('Email is required.');
		expect(validateForgotEmail('not-an-email')).toBe('Email must be a valid email address.');
		expect(validateForgotEmail(`${'a'.repeat(310)}@example.test`)).toBe(
			'Email must be at most 320 characters.'
		);
		expect(validateForgotEmail('user@example.test')).toBeUndefined();
	});

	it('validates reset password', () => {
		expect(validateResetPassword('')).toBe('Password is required.');
		expect(validateResetPassword('short')).toBe('Password must be at least 12 characters.');
		expect(validateResetPassword('x'.repeat(129))).toBe('Password must be at most 128 characters.');
		expect(validateResetPassword('long-enough-password')).toBeUndefined();
	});

	it('parses Reset-Available-In header seconds', () => {
		expect(readResetAvailableInSeconds(undefined)).toBeUndefined();
		expect(readResetAvailableInSeconds(new Headers())).toBeUndefined();
		expect(
			readResetAvailableInSeconds(new Headers({ [RESET_AVAILABLE_IN_HEADER]: 'not-a-number' }))
		).toBeUndefined();
		expect(readResetAvailableInSeconds(new Headers({ [RESET_AVAILABLE_IN_HEADER]: '-1' }))).toBe(
			undefined
		);
		expect(readResetAvailableInSeconds(new Headers({ [RESET_AVAILABLE_IN_HEADER]: '0' }))).toBe(0);
		expect(readResetAvailableInSeconds(new Headers({ [RESET_AVAILABLE_IN_HEADER]: '1200' }))).toBe(
			1200
		);
		expect(
			readResetAvailableInSeconds(new Headers({ [RESET_AVAILABLE_IN_HEADER]: '1800.9' }))
		).toBe(1800);
	});

	it('posts forgot-password body and returns message + remaining cooldown', async () => {
		post.mockResolvedValue({
			data: 'custom forgot message',
			response: new Response(null, {
				status: 200,
				headers: { [RESET_AVAILABLE_IN_HEADER]: '1500' }
			})
		});

		await expect(postForgotPassword('user@example.test')).resolves.toEqual({
			message: 'custom forgot message',
			resetAvailableInSeconds: 1500
		});
		expect(post).toHaveBeenCalledWith('/identities/forgot-password', {
			body: { email: 'user@example.test' }
		});
	});

	it('falls back when forgot-password body is empty and header is absent', async () => {
		post.mockResolvedValue({
			data: '',
			response: new Response(null, { status: 200 })
		});

		await expect(postForgotPassword('user@example.test')).resolves.toEqual({
			message: FORGOT_PASSWORD_SUCCESS_FALLBACK,
			resetAvailableInSeconds: undefined
		});
	});

	it('posts reset-password path and body', async () => {
		post.mockResolvedValue({ data: 'custom reset message' });

		await expect(postResetPassword('abc123', 'long-enough-password')).resolves.toBe(
			'custom reset message'
		);
		expect(post).toHaveBeenCalledWith('/identities/reset-password/{resetCode}', {
			params: { path: { resetCode: 'abc123' } },
			body: { password: 'long-enough-password' }
		});
	});

	it('falls back when reset-password body is empty', async () => {
		post.mockResolvedValue({ data: null });

		await expect(postResetPassword('abc123', 'long-enough-password')).resolves.toBe(
			RESET_PASSWORD_SUCCESS_FALLBACK
		);
	});

	it('propagates ApiError from Identity', async () => {
		post.mockRejectedValue(new ApiError(503, { status: 503, title: 'down' }));

		await expect(postForgotPassword('user@example.test')).rejects.toBeInstanceOf(ApiError);
	});
});
