import { createIdentityApi } from './identity';

/** Matches Identity `Identities.ResendVerification.Endpoint.SuccessMessage`. */
export const RESEND_VERIFICATION_SUCCESS_FALLBACK =
	'If an account needs verification, we sent a link.';

/** Matches Identity login rejection for non-Active accounts. */
export const ACCOUNT_NOT_VERIFIED_MESSAGE = 'Account not verified.';

/** Matches Identity `VerificationResend.AvailableInHeaderName` (seconds remaining). */
export const RESEND_AVAILABLE_IN_HEADER = 'Resend-Available-In';

/** Parse remaining resend cooldown seconds from an Identity error response. */
export function readResendAvailableInSeconds(
	headers: Headers | undefined | null
): number | undefined {
	if (!headers) return undefined;
	const raw = headers.get(RESEND_AVAILABLE_IN_HEADER);
	if (raw == null || raw.trim() === '') return undefined;
	const seconds = Number(raw);
	if (!Number.isFinite(seconds) || seconds < 0) return undefined;
	return Math.floor(seconds);
}

export async function postResendVerification(email: string): Promise<string> {
	const { data } = await createIdentityApi().POST('/identities/resend-verification', {
		body: { email }
	});

	return typeof data === 'string' && data.length > 0 ? data : RESEND_VERIFICATION_SUCCESS_FALLBACK;
}

export function validateResendEmail(email: string): string | undefined {
	if (!email) return 'Email is required.';
	if (email.length > 320) return 'Email must be at most 320 characters.';
	if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Email must be a valid email address.';
	return undefined;
}
