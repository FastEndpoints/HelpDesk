import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createIdentityApi } from './identity';
import {
	postResendVerification,
	readResendAvailableInSeconds,
	RESEND_AVAILABLE_IN_HEADER,
	RESEND_VERIFICATION_SUCCESS_FALLBACK,
	validateResendEmail
} from './resend-verification';

vi.mock('./identity', () => ({
	createIdentityApi: vi.fn()
}));

const createIdentityApiMock = vi.mocked(createIdentityApi);
const post = vi.fn();

describe('readResendAvailableInSeconds', () => {
	it('reads a non-negative integer header value', () => {
		const headers = new Headers({ [RESEND_AVAILABLE_IN_HEADER]: '1200' });
		expect(readResendAvailableInSeconds(headers)).toBe(1200);
	});

	it('floors fractional values and accepts zero', () => {
		expect(readResendAvailableInSeconds(new Headers({ [RESEND_AVAILABLE_IN_HEADER]: '0' }))).toBe(
			0
		);
		expect(
			readResendAvailableInSeconds(new Headers({ [RESEND_AVAILABLE_IN_HEADER]: '12.9' }))
		).toBe(12);
	});

	it('returns undefined for missing or invalid values', () => {
		expect(readResendAvailableInSeconds(undefined)).toBeUndefined();
		expect(readResendAvailableInSeconds(new Headers())).toBeUndefined();
		expect(
			readResendAvailableInSeconds(new Headers({ [RESEND_AVAILABLE_IN_HEADER]: '' }))
		).toBeUndefined();
		expect(
			readResendAvailableInSeconds(new Headers({ [RESEND_AVAILABLE_IN_HEADER]: '-1' }))
		).toBeUndefined();
		expect(
			readResendAvailableInSeconds(new Headers({ [RESEND_AVAILABLE_IN_HEADER]: 'nope' }))
		).toBeUndefined();
	});
});

describe('validateResendEmail', () => {
	it('requires a non-empty email', () => {
		expect(validateResendEmail('')).toBe('Email is required.');
	});

	it('rejects emails over 320 characters', () => {
		const email = `${'a'.repeat(310)}@example.com`;
		expect(email.length).toBeGreaterThan(320);
		expect(validateResendEmail(email)).toBe('Email must be at most 320 characters.');
	});

	it('rejects invalid email shapes', () => {
		expect(validateResendEmail('not-an-email')).toBe('Email must be a valid email address.');
	});

	it('accepts a normal email', () => {
		expect(validateResendEmail('user@example.test')).toBeUndefined();
	});
});

describe('postResendVerification', () => {
	beforeEach(() => {
		post.mockReset();
		createIdentityApiMock.mockReset();
		createIdentityApiMock.mockReturnValue({ POST: post } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('returns the Identity success body', async () => {
		post.mockResolvedValue({
			data: 'If an account needs verification, we sent a link.',
			error: undefined,
			response: new Response()
		});

		await expect(postResendVerification('user@example.test')).resolves.toBe(
			'If an account needs verification, we sent a link.'
		);
		expect(post).toHaveBeenCalledWith('/identities/resend-verification', {
			body: { email: 'user@example.test' }
		});
	});

	it('falls back when Identity returns an empty body', async () => {
		post.mockResolvedValue({ data: '', error: undefined, response: new Response() });

		await expect(postResendVerification('user@example.test')).resolves.toBe(
			RESEND_VERIFICATION_SUCCESS_FALLBACK
		);
	});
});
