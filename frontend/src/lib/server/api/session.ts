import type { Cookies } from '@sveltejs/kit';

const COOKIE_NAME = 'helpdesk_session';
const DEFAULT_MAX_AGE_SECONDS = 7 * 24 * 60 * 60;

export function readSessionToken(cookies: Cookies): string | undefined {
	return cookies.get(COOKIE_NAME);
}

export function writeSessionToken(
	cookies: Cookies,
	token: string,
	maxAge = DEFAULT_MAX_AGE_SECONDS
): void {
	cookies.set(COOKIE_NAME, token, {
		httpOnly: true,
		secure: process.env.NODE_ENV === 'production',
		sameSite: 'lax',
		path: '/',
		maxAge: Math.min(maxAge, DEFAULT_MAX_AGE_SECONDS)
	});
}

export function clearSessionToken(cookies: Cookies): void {
	cookies.delete(COOKIE_NAME, {
		httpOnly: true,
		secure: process.env.NODE_ENV === 'production',
		sameSite: 'lax',
		path: '/'
	});
}
