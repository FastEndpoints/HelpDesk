import { describe, expect, it } from 'vitest';
import {
	formatResendCountdown,
	isResendButtonDisabled,
	RESEND_COOLDOWN_MS,
	resendButtonLabel,
	resendCooldownDurationMs,
	shouldShowResendCountdown,
	shouldStartResendCooldown
} from './resend-cooldown';

describe('resend cooldown helpers', () => {
	it('matches the Identity 30-minute resend window', () => {
		expect(RESEND_COOLDOWN_MS).toBe(30 * 60 * 1000);
	});

	it('formats remaining time as m:ss', () => {
		expect(formatResendCountdown(0)).toBe('0:00');
		expect(formatResendCountdown(1000)).toBe('0:01');
		expect(formatResendCountdown(59_001)).toBe('1:00');
		expect(formatResendCountdown(RESEND_COOLDOWN_MS)).toBe('30:00');
		expect(formatResendCountdown(29 * 60 * 1000 + 5_000)).toBe('29:05');
	});

	describe('shouldStartResendCooldown', () => {
		it('register starts on success card and skips when already started', () => {
			expect(
				shouldStartResendCooldown({
					surface: 'register',
					success: true,
					cooldownAlreadyStarted: false
				})
			).toBe(true);
			expect(
				shouldStartResendCooldown({
					surface: 'register',
					success: true,
					cooldownAlreadyStarted: true
				})
			).toBe(false);
			expect(
				shouldStartResendCooldown({
					surface: 'register',
					success: false,
					resendSuccess: true,
					cooldownAlreadyStarted: false
				})
			).toBe(false);
		});

		it('login starts from server remaining seconds or after resendSuccess', () => {
			expect(
				shouldStartResendCooldown({
					surface: 'login',
					success: false,
					resendSuccess: false,
					cooldownAlreadyStarted: false
				})
			).toBe(false);
			expect(
				shouldStartResendCooldown({
					surface: 'login',
					resendAvailableInSeconds: 0,
					cooldownAlreadyStarted: false
				})
			).toBe(true);
			expect(
				shouldStartResendCooldown({
					surface: 'login',
					resendAvailableInSeconds: 1200,
					cooldownAlreadyStarted: false
				})
			).toBe(true);
			expect(
				shouldStartResendCooldown({
					surface: 'login',
					resendAvailableInSeconds: 1200,
					cooldownAlreadyStarted: true
				})
			).toBe(false);
			expect(
				shouldStartResendCooldown({
					surface: 'login',
					resendSuccess: true,
					cooldownAlreadyStarted: false
				})
			).toBe(true);
			expect(
				shouldStartResendCooldown({
					surface: 'login',
					resendSuccess: true,
					cooldownAlreadyStarted: true
				})
			).toBe(false);
		});
	});

	describe('resendCooldownDurationMs', () => {
		it('uses full window after resendSuccess, else server seconds, else full window', () => {
			expect(resendCooldownDurationMs({ resendSuccess: true, resendAvailableInSeconds: 10 })).toBe(
				RESEND_COOLDOWN_MS
			);
			expect(resendCooldownDurationMs({ resendAvailableInSeconds: 1200 })).toBe(1_200_000);
			expect(resendCooldownDurationMs({ resendAvailableInSeconds: 0 })).toBe(0);
			expect(resendCooldownDurationMs({})).toBe(RESEND_COOLDOWN_MS);
		});
	});

	describe('shouldShowResendCountdown', () => {
		it('shows countdown whenever remaining time is positive', () => {
			expect(shouldShowResendCountdown({ remainingMs: 1 })).toBe(true);
			expect(shouldShowResendCountdown({ remainingMs: RESEND_COOLDOWN_MS })).toBe(true);
			expect(shouldShowResendCountdown({ remainingMs: 0 })).toBe(false);
		});
	});

	describe('resendButtonLabel', () => {
		it('uses pending, first-send, and send-again labels', () => {
			expect(resendButtonLabel({ pending: true })).toBe('Sending…');
			expect(resendButtonLabel({ pending: false, resendSuccess: false })).toBe(
				'Resend verification email'
			);
			expect(resendButtonLabel({ pending: false, resendSuccess: true })).toBe('Send again');
			expect(resendButtonLabel({ pending: true, resendSuccess: true })).toBe('Sending…');
		});
	});

	describe('isResendButtonDisabled', () => {
		it('disables while pending, cooling down, or missing email', () => {
			expect(
				isResendButtonDisabled({ pending: false, remainingMs: 0, email: 'a@b.c' })
			).toBe(false);
			expect(
				isResendButtonDisabled({ pending: true, remainingMs: 0, email: 'a@b.c' })
			).toBe(true);
			expect(
				isResendButtonDisabled({ pending: false, remainingMs: 1, email: 'a@b.c' })
			).toBe(true);
			expect(isResendButtonDisabled({ pending: false, remainingMs: 0, email: '' })).toBe(
				true
			);
			expect(
				isResendButtonDisabled({ pending: false, remainingMs: 0, email: null })
			).toBe(true);
		});
	});
});
