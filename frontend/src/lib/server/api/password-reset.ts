import { createIdentityApi } from './identity';

/** Matches Identity `Identities.ForgotPassword.Endpoint.SuccessMessage`. */
export const FORGOT_PASSWORD_SUCCESS_FALLBACK =
	'If an account exists for that email, we sent a reset link.';

/** Matches Identity `Identities.ResetPassword.Endpoint.SuccessMessage`. */
export const RESET_PASSWORD_SUCCESS_FALLBACK = 'Password updated. You can sign in.';

/** Matches Identity `PasswordReset.AvailableInHeaderName` (seconds remaining). */
export const RESET_AVAILABLE_IN_HEADER = 'Reset-Available-In';

export type ForgotPasswordResult = {
	message: string;
	/** Remaining request cooldown seconds from Identity, when present. */
	resetAvailableInSeconds?: number;
};

/** Parse remaining reset-request cooldown seconds from an Identity success response. */
export function readResetAvailableInSeconds(
	headers: Headers | undefined | null
): number | undefined {
	if (!headers) return undefined;
	const raw = headers.get(RESET_AVAILABLE_IN_HEADER);
	if (raw == null || raw.trim() === '') return undefined;
	const seconds = Number(raw);
	if (!Number.isFinite(seconds) || seconds < 0) return undefined;
	return Math.floor(seconds);
}

export async function postForgotPassword(email: string): Promise<ForgotPasswordResult> {
	const { data, response } = await createIdentityApi().POST('/identities/forgot-password', {
		body: { email }
	});

	return {
		message: typeof data === 'string' && data.length > 0 ? data : FORGOT_PASSWORD_SUCCESS_FALLBACK,
		resetAvailableInSeconds: readResetAvailableInSeconds(response.headers)
	};
}

export async function postResetPassword(resetCode: string, password: string): Promise<string> {
	const { data } = await createIdentityApi().POST('/identities/reset-password/{resetCode}', {
		params: { path: { resetCode } },
		body: { password }
	});

	return typeof data === 'string' && data.length > 0 ? data : RESET_PASSWORD_SUCCESS_FALLBACK;
}

export function validateForgotEmail(email: string): string | undefined {
	if (!email) return 'Email is required.';
	if (email.length > 320) return 'Email must be at most 320 characters.';
	if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Email must be a valid email address.';
	return undefined;
}

export function validateResetPassword(password: string): string | undefined {
	if (!password) return 'Password is required.';
	if (password.length < 12) return 'Password must be at least 12 characters.';
	if (password.length > 128) return 'Password must be at most 128 characters.';
	return undefined;
}
