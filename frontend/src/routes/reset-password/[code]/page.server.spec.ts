import { isActionFailure } from '@sveltejs/kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import { postResetPassword, RESET_PASSWORD_SUCCESS_FALLBACK } from '$lib/server/api/password-reset';
import { actions, load, type ResetPasswordFormState } from './+page.server';

vi.mock('$lib/server/api/password-reset', async (importOriginal) => {
	const actual = await importOriginal<typeof import('$lib/server/api/password-reset')>();
	return {
		...actual,
		postResetPassword: vi.fn()
	};
});

const postResetPasswordMock = vi.mocked(postResetPassword);

function requestOf(fields: Record<string, string>): Request {
	const body = new FormData();
	for (const [key, value] of Object.entries(fields)) {
		body.set(key, value);
	}
	return new Request('http://frontend.test/reset-password/abc', { method: 'POST', body });
}

async function submit(code: string, fields: Record<string, string>) {
	const action = actions.reset;
	if (typeof action !== 'function') throw new Error('reset action missing');
	return action({ request: requestOf(fields), params: { code } } as never);
}

function expectFailure(result: unknown): { status: number; data: ResetPasswordFormState } {
	expect(isActionFailure(result)).toBe(true);
	if (!isActionFailure(result) || result.data === undefined) {
		throw new Error('expected ActionFailure with data');
	}
	return { status: result.status, data: result.data as ResetPasswordFormState };
}

describe('reset-password form action', () => {
	beforeEach(() => {
		postResetPasswordMock.mockReset();
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('exposes the code from the route for the page load', async () => {
		await expect(load({ params: { code: '  abc123  ' } } as never)).resolves.toEqual({
			code: 'abc123',
			hasCode: true
		});
	});

	it('marks empty codes as missing on load', async () => {
		await expect(load({ params: { code: '   ' } } as never)).resolves.toEqual({
			code: '',
			hasCode: false
		});
	});

	it('rejects missing code before calling Identity', async () => {
		const result = expectFailure(
			await submit('', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors.form).toEqual(['Reset code is missing.']);
		expect(postResetPasswordMock).not.toHaveBeenCalled();
	});

	it('rejects short password and mismatch before calling Identity', async () => {
		const result = expectFailure(
			await submit('abc123', {
				password: 'short',
				confirmPassword: 'different'
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			password: ['Password must be at least 12 characters.'],
			confirmPassword: ['Passwords do not match.']
		});
		expect(postResetPasswordMock).not.toHaveBeenCalled();
	});

	it('rejects empty confirm password and overlong password before calling Identity', async () => {
		const emptyConfirm = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: ''
			})
		);

		expect(emptyConfirm.status).toBe(400);
		expect(emptyConfirm.data.errors).toEqual({
			confirmPassword: ['Confirm your password.']
		});

		const overlong = expectFailure(
			await submit('abc123', {
				password: 'x'.repeat(129),
				confirmPassword: 'x'.repeat(129)
			})
		);

		expect(overlong.status).toBe(400);
		expect(overlong.data.errors).toEqual({
			password: ['Password must be at most 128 characters.']
		});
		expect(postResetPasswordMock).not.toHaveBeenCalled();
	});

	it('trims the path code and returns success message', async () => {
		postResetPasswordMock.mockResolvedValue(RESET_PASSWORD_SUCCESS_FALLBACK);

		const result = await submit('  abc123  ', {
			password: 'long-enough-password',
			confirmPassword: 'long-enough-password'
		});

		expect(isActionFailure(result)).toBe(false);
		expect(result).toMatchObject({
			success: true,
			message: RESET_PASSWORD_SUCCESS_FALLBACK,
			errors: {}
		});
		expect(postResetPasswordMock).toHaveBeenCalledWith('abc123', 'long-enough-password');
	});

	it('maps field ApiError from Identity', async () => {
		postResetPasswordMock.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more validation errors occurred.',
				errors: [{ name: 'password', reason: 'Password is too short.' }]
			})
		);

		const result = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.data.errors.password).toEqual(['Password is too short.']);
	});

	it('maps non-field ApiError detail to form error', async () => {
		postResetPasswordMock.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				detail: 'Invalid or expired reset link.'
			})
		);

		const result = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.data.errors.form).toEqual(['Invalid or expired reset link.']);
	});

	it('falls back to problem title when detail is missing', async () => {
		postResetPasswordMock.mockRejectedValue(
			new ApiError(401, { status: 401, title: 'Unauthorized' })
		);

		const result = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.status).toBe(401);
		expect(result.data.errors.form).toEqual(['Unauthorized']);
	});

	it('falls back to a generic form error when problem has neither detail nor title', async () => {
		postResetPasswordMock.mockRejectedValue(new ApiError(400, { status: 400 }));

		const result = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors.form).toEqual(['Password reset failed. Please try again.']);
	});

	it('clamps out-of-range ApiError status to 400', async () => {
		postResetPasswordMock.mockRejectedValue(
			new ApiError(399, { status: 399, detail: 'Weird status' })
		);

		const result = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors.form).toEqual(['Weird status']);
	});

	it('returns 500 when Identity is unreachable', async () => {
		postResetPasswordMock.mockRejectedValue(new Error('network'));

		const result = expectFailure(
			await submit('abc123', {
				password: 'long-enough-password',
				confirmPassword: 'long-enough-password'
			})
		);

		expect(result.status).toBe(500);
		expect(result.data.errors.form).toEqual([
			'Unable to reach the identity service. Please try again later.'
		]);
	});
});
