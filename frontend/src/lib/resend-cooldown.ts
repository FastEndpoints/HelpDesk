/** Matches Identity `VerificationResend.Cooldown` / `PasswordReset.RequestCooldown` (client fallback). */
export const RESEND_COOLDOWN_MS = 30 * 60 * 1000;

export type ResendSurface = 'register' | 'login' | 'forgot-password';

export function formatResendCountdown(ms: number): string {
	const totalSeconds = Math.ceil(ms / 1000);
	const minutes = Math.floor(totalSeconds / 60);
	const seconds = totalSeconds % 60;
	return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

/**
 * Whether to begin the client-only cooldown timer for a surface.
 * Register: starts with the check-email success card (verification just issued).
 * Login: starts from server remaining seconds (needs-verification) or after successful resend.
 * Forgot-password: starts with the success card (seeded from `Reset-Available-In`).
 */
export function shouldStartResendCooldown(input: {
	surface: ResendSurface;
	success?: boolean;
	resendSuccess?: boolean;
	resendAvailableInSeconds?: number | null;
	cooldownAlreadyStarted: boolean;
}): boolean {
	if (input.cooldownAlreadyStarted) return false;
	if (input.surface === 'register' || input.surface === 'forgot-password') {
		return Boolean(input.success);
	}
	if (input.resendSuccess) return true;
	return input.resendAvailableInSeconds != null;
}

/** Duration to apply when starting/restarting the client timer. */
export function resendCooldownDurationMs(input: {
	/** Kept for call-site compatibility; server remaining seconds take priority. */
	resendSuccess?: boolean;
	resendAvailableInSeconds?: number | null;
}): number {
	// Prefer server remaining seconds when present (forgot-password / login header).
	// Full window when no server value (register success, login resend without header).
	if (input.resendAvailableInSeconds != null) {
		return Math.max(0, input.resendAvailableInSeconds) * 1000;
	}
	return RESEND_COOLDOWN_MS;
}

/** Live countdown copy whenever remaining time is positive. */
export function shouldShowResendCountdown(input: { remainingMs: number }): boolean {
	return input.remainingMs > 0;
}

export function resendButtonLabel(input: {
	pending: boolean;
	resendSuccess?: boolean;
	surface?: ResendSurface;
}): string {
	if (input.pending) return 'Sending…';
	if (input.surface === 'forgot-password') return 'Send again';
	return input.resendSuccess ? 'Send again' : 'Resend verification email';
}

export function isResendButtonDisabled(input: {
	pending: boolean;
	remainingMs: number;
	email?: string | null;
}): boolean {
	return input.pending || input.remainingMs > 0 || !input.email;
}
