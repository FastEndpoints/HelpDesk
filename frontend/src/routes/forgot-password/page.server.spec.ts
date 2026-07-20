import { isActionFailure } from '@sveltejs/kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import {
	FORGOT_PASSWORD_SUCCESS_FALLBACK,
	postForgotPassword
} from '$lib/server/api/password-reset';
import { actions, type ForgotPasswordFormState } from './+page.server';

vi.mock('$lib/server/api/password-reset', async (importOriginal) => {
	const actual = await importOriginal<typeof import('$lib/server/api/password-reset')>();
	return {
		...actual,
		postForgotPassword: vi.fn()
	};
});

const postForgotPasswordMock = vi.mocked(postForgotPassword);

function requestOf(fields: Record<string, string>): Request {
	const body = new FormData();
	for (const [key, value] of Object.entries(fields)) {
		body.set(key, value);
	}
	return new Request('http://frontend.test/forgot-password', { method: 'POST', body });
}

async function submit(fields: Record<string, string>) {
	const action = actions.request;
	if (typeof action !== 'function') throw new Error('request action missing');
	return action({ request: requestOf(fields) } as never);
}

function expectFailure(result: unknown): { status: number; data: ForgotPasswordFormState } {
	expect(isActionFailure(result)).toBe(true);
	if (!isActionFailure(result) || result.data === undefined) {
		throw new Error('expected ActionFailure with data');
	}
	return { status: result.status, data: result.data as ForgotPasswordFormState };
}

describe('forgot-password form action', () => {
	beforeEach(() => {
		postForgotPasswordMock.mockReset();
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('rejects missing email before calling Identity', async () => {
		const result = expectFailure(await submit({ email: '' }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			values: { email: '' },
			errors: { email: ['Email is required.'] }
		});
		expect(postForgotPasswordMock).not.toHaveBeenCalled();
	});

	it('rejects invalid and oversized email', async () => {
		expect(expectFailure(await submit({ email: 'not-an-email' })).data.errors.email).toEqual([
			'Email must be a valid email address.'
		]);
		expect(
			expectFailure(await submit({ email: `${'a'.repeat(310)}@example.test` })).data.errors.email
		).toEqual(['Email must be at most 320 characters.']);
		expect(postForgotPasswordMock).not.toHaveBeenCalled();
	});

	it('trims email and returns success message with cooldown seconds', async () => {
		postForgotPasswordMock.mockResolvedValue({
			message: FORGOT_PASSWORD_SUCCESS_FALLBACK,
			resetAvailableInSeconds: 1500
		});

		const result = await submit({ email: '  user@example.test  ' });

		expect(isActionFailure(result)).toBe(false);
		expect(result).toMatchObject({
			success: true,
			message: FORGOT_PASSWORD_SUCCESS_FALLBACK,
			resetAvailableInSeconds: 1500,
			values: { email: 'user@example.test' },
			errors: {}
		});
		expect(postForgotPasswordMock).toHaveBeenCalledWith('user@example.test');
	});

	it('falls back to full 30m when Identity omits Reset-Available-In', async () => {
		postForgotPasswordMock.mockResolvedValue({
			message: FORGOT_PASSWORD_SUCCESS_FALLBACK,
			resetAvailableInSeconds: undefined
		});

		const result = await submit({ email: 'user@example.test' });

		expect(result).toMatchObject({
			success: true,
			resetAvailableInSeconds: 1800
		});
	});

	it('maps field ApiError from Identity', async () => {
		postForgotPasswordMock.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more validation errors occurred.',
				errors: [{ name: 'email', reason: 'Email is invalid.' }]
			})
		);

		const result = expectFailure(await submit({ email: 'user@example.test' }));

		expect(result.status).toBe(400);
		expect(result.data.errors.email).toEqual(['Email is invalid.']);
	});

	it('maps non-field ApiError detail to form error', async () => {
		postForgotPasswordMock.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				detail: 'Frontend base URL is not configured.'
			})
		);

		const result = expectFailure(await submit({ email: 'user@example.test' }));

		expect(result.data.errors.form).toEqual(['Frontend base URL is not configured.']);
	});

	it('falls back to problem title when detail is missing', async () => {
		postForgotPasswordMock.mockRejectedValue(
			new ApiError(401, { status: 401, title: 'Unauthorized' })
		);

		const result = expectFailure(await submit({ email: 'user@example.test' }));

		expect(result.status).toBe(401);
		expect(result.data.errors.form).toEqual(['Unauthorized']);
	});

	it('falls back to a generic form error when problem has neither detail nor title', async () => {
		postForgotPasswordMock.mockRejectedValue(new ApiError(400, { status: 400 }));

		const result = expectFailure(await submit({ email: 'user@example.test' }));

		expect(result.status).toBe(400);
		expect(result.data.errors.form).toEqual(['Unable to send reset email. Please try again.']);
	});

	it('clamps out-of-range ApiError status to 400', async () => {
		postForgotPasswordMock.mockRejectedValue(
			new ApiError(399, { status: 399, detail: 'Weird status' })
		);

		const result = expectFailure(await submit({ email: 'user@example.test' }));

		expect(result.status).toBe(400);
		expect(result.data.errors.form).toEqual(['Weird status']);
	});

	it('returns 500 when Identity is unreachable', async () => {
		postForgotPasswordMock.mockRejectedValue(new Error('network'));

		const result = expectFailure(await submit({ email: 'user@example.test' }));

		expect(result.status).toBe(500);
		expect(result.data.errors.form).toEqual([
			'Unable to reach the identity service. Please try again later.'
		]);
	});
});
