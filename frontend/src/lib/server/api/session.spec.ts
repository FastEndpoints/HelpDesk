import { afterEach, describe, expect, it, vi } from 'vitest';
import { clearSessionToken, readSessionToken, writeSessionToken } from './session';

function cookiesMock() {
	return {
		get: vi.fn(),
		set: vi.fn(),
		delete: vi.fn()
	};
}

describe('session cookie helpers', () => {
	const originalNodeEnv = process.env.NODE_ENV;

	afterEach(() => {
		process.env.NODE_ENV = originalNodeEnv;
		vi.restoreAllMocks();
	});

	it('reads helpdesk_session from cookies', () => {
		const cookies = cookiesMock();
		cookies.get.mockReturnValue('jwt-token');

		expect(readSessionToken(cookies as never)).toBe('jwt-token');
		expect(cookies.get).toHaveBeenCalledWith('helpdesk_session');
	});

	it('returns undefined when the session cookie is absent', () => {
		const cookies = cookiesMock();
		cookies.get.mockReturnValue(undefined);

		expect(readSessionToken(cookies as never)).toBeUndefined();
	});

	it('writes an HttpOnly Lax session cookie with the default maxAge', () => {
		process.env.NODE_ENV = 'development';
		const cookies = cookiesMock();

		writeSessionToken(cookies as never, 'jwt-access-token');

		expect(cookies.set).toHaveBeenCalledWith('helpdesk_session', 'jwt-access-token', {
			httpOnly: true,
			secure: false,
			sameSite: 'lax',
			path: '/',
			maxAge: 7 * 24 * 60 * 60
		});
	});

	it('caps maxAge at seven days', () => {
		process.env.NODE_ENV = 'development';
		const cookies = cookiesMock();

		writeSessionToken(cookies as never, 'jwt-access-token', 30 * 24 * 60 * 60);

		expect(cookies.set).toHaveBeenCalledWith(
			'helpdesk_session',
			'jwt-access-token',
			expect.objectContaining({ maxAge: 7 * 24 * 60 * 60 })
		);
	});

	it('preserves a shorter maxAge', () => {
		process.env.NODE_ENV = 'development';
		const cookies = cookiesMock();

		writeSessionToken(cookies as never, 'jwt-access-token', 3600);

		expect(cookies.set).toHaveBeenCalledWith(
			'helpdesk_session',
			'jwt-access-token',
			expect.objectContaining({ maxAge: 3600 })
		);
	});

	it('sets Secure in production', () => {
		process.env.NODE_ENV = 'production';
		const cookies = cookiesMock();

		writeSessionToken(cookies as never, 'jwt-access-token', 60);

		expect(cookies.set).toHaveBeenCalledWith(
			'helpdesk_session',
			'jwt-access-token',
			expect.objectContaining({
				httpOnly: true,
				secure: true,
				sameSite: 'lax',
				path: '/'
			})
		);
	});

	it('clears the session cookie with matching security attributes', () => {
		process.env.NODE_ENV = 'production';
		const cookies = cookiesMock();

		clearSessionToken(cookies as never);

		expect(cookies.delete).toHaveBeenCalledWith('helpdesk_session', {
			httpOnly: true,
			secure: true,
			sameSite: 'lax',
			path: '/'
		});
	});

	it('clears without Secure outside production', () => {
		process.env.NODE_ENV = 'test';
		const cookies = cookiesMock();

		clearSessionToken(cookies as never);

		expect(cookies.delete).toHaveBeenCalledWith('helpdesk_session', {
			httpOnly: true,
			secure: false,
			sameSite: 'lax',
			path: '/'
		});
	});
});
