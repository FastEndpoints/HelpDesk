import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createIdentityApi } from '$lib/server/api/identity';
import { ApiError } from '$lib/server/api/errors';
import { mapProblemFieldErrors } from '$lib/server/api/problem';
import {
	ACCOUNT_NOT_VERIFIED_MESSAGE,
	postResendVerification,
	readResendAvailableInSeconds,
	RESEND_VERIFICATION_SUCCESS_FALLBACK,
	validateResendEmail
} from '$lib/server/api/resend-verification';
import { writeSessionToken } from '$lib/server/api/session';
import { RESEND_COOLDOWN_MS } from '$lib/resend-cooldown';

export type LoginFieldErrors = {
	email?: string[];
	password?: string[];
	form?: string[];
	resend?: string[];
};

export type LoginFormState = {
	success: boolean;
	message?: string;
	resendSuccess?: boolean;
	needsVerification?: boolean;
	/** Remaining resend cooldown from Identity (`Resend-Available-In`), when known. */
	resendAvailableInSeconds?: number;
	values: {
		email: string;
	};
	errors: LoginFieldErrors;
};

function formState(
	partial: Omit<LoginFormState, 'values' | 'errors'> &
		Partial<
			Pick<
				LoginFormState,
				'values' | 'errors' | 'resendSuccess' | 'needsVerification' | 'resendAvailableInSeconds'
			>
		> & {
			values?: { email?: string };
		}
): LoginFormState {
	return {
		success: partial.success,
		message: partial.message,
		resendSuccess: partial.resendSuccess,
		needsVerification: partial.needsVerification,
		resendAvailableInSeconds: partial.resendAvailableInSeconds,
		values: { email: partial.values?.email ?? '' },
		errors: partial.errors ?? {}
	};
}

function sessionMaxAgeSeconds(expiresAt: string | Date | undefined): number | undefined {
	if (expiresAt == null) return undefined;
	const expiresMs = typeof expiresAt === 'string' ? Date.parse(expiresAt) : expiresAt.getTime();
	if (Number.isNaN(expiresMs)) return undefined;
	return Math.max(0, Math.floor((expiresMs - Date.now()) / 1000));
}

function safeRedirectPath(raw: FormDataEntryValue | null | undefined): string {
	if (typeof raw !== 'string') return '/';
	const path = raw.trim();
	if (!path.startsWith('/') || path.startsWith('//') || path.includes('://')) return '/';
	return path;
}

function isAccountNotVerified(detail: string | undefined, title: string | undefined): boolean {
	return detail === ACCOUNT_NOT_VERIFIED_MESSAGE || title === ACCOUNT_NOT_VERIFIED_MESSAGE;
}

export const load: PageServerLoad = async ({ url }) => {
	return {
		redirectTo: safeRedirectPath(url.searchParams.get('redirectTo'))
	};
};

export const actions: Actions = {
	login: async ({ request, cookies }) => {
		const formData = await request.formData();
		const email = String(formData.get('email') ?? '').trim();
		const password = String(formData.get('password') ?? '');
		const redirectTo = safeRedirectPath(formData.get('redirectTo'));

		const errors: LoginFieldErrors = {};

		if (!email) {
			errors.email = ['Email is required.'];
		} else if (email.length > 320) {
			errors.email = ['Email must be at most 320 characters.'];
		} else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
			errors.email = ['Email must be a valid email address.'];
		}

		if (!password) {
			errors.password = ['Password is required.'];
		} else if (password.length > 128) {
			errors.password = ['Password must be at most 128 characters.'];
		}

		if (Object.keys(errors).length > 0) {
			return fail(400, formState({ success: false, values: { email }, errors }));
		}

		let accessToken: string;
		let maxAge: number | undefined;

		try {
			const { data } = await createIdentityApi().POST('/identities/login', {
				body: { email, password }
			});

			if (!data?.accessToken) {
				return fail(
					502,
					formState({
						success: false,
						values: { email },
						errors: {
							form: ['Login succeeded but no access token was returned. Please try again.']
						}
					})
				);
			}

			accessToken = data.accessToken;
			maxAge = sessionMaxAgeSeconds(data.expiresAt);
		} catch (error) {
			if (error instanceof ApiError) {
				const fieldErrors = mapProblemFieldErrors(error.problem);
				const mapped: LoginFieldErrors = {};

				if (fieldErrors.email?.length) mapped.email = fieldErrors.email;
				if (fieldErrors.password?.length) mapped.password = fieldErrors.password;

				const detail = error.problem.detail;
				const title = error.problem.title;
				const needsVerification =
					!mapped.email && !mapped.password && isAccountNotVerified(detail, title);
				const resendAvailableInSeconds = needsVerification
					? readResendAvailableInSeconds(error.headers)
					: undefined;

				if (!mapped.email && !mapped.password) {
					mapped.form = [
						detail ?? title ?? 'Sign-in failed. Please try again.'
					];
				}

				return fail(
					error.status >= 400 && error.status < 600 ? error.status : 400,
					formState({
						success: false,
						values: { email },
						errors: mapped,
						message: detail ?? title,
						needsVerification,
						resendAvailableInSeconds
					})
				);
			}

			return fail(
				500,
				formState({
					success: false,
					values: { email },
					errors: {
						form: ['Unable to reach the identity service. Please try again later.']
					}
				})
			);
		}

		writeSessionToken(cookies, accessToken, maxAge);
		redirect(303, redirectTo);
	},

	resend: async ({ request }) => {
		const formData = await request.formData();
		const email = String(formData.get('email') ?? '').trim();
		const emailError = validateResendEmail(email);

		if (emailError) {
			return fail(
				400,
				formState({
					success: false,
					values: { email },
					needsVerification: true,
					errors: {
						form: [ACCOUNT_NOT_VERIFIED_MESSAGE],
						resend: [emailError]
					}
				})
			);
		}

		try {
			const message = await postResendVerification(email);

			return formState({
				success: false,
				resendSuccess: true,
				needsVerification: true,
				resendAvailableInSeconds: Math.ceil(RESEND_COOLDOWN_MS / 1000),
				message,
				values: { email },
				errors: {
					form: [ACCOUNT_NOT_VERIFIED_MESSAGE]
				}
			});
		} catch (error) {
			if (error instanceof ApiError) {
				const fieldErrors = mapProblemFieldErrors(error.problem);
				const resendError =
					fieldErrors.email?.[0] ??
					error.problem.detail ??
					error.problem.title ??
					'Unable to resend verification email. Please try again.';

				return fail(
					error.status >= 400 && error.status < 600 ? error.status : 400,
					formState({
						success: false,
						values: { email },
						needsVerification: true,
						errors: {
							form: [ACCOUNT_NOT_VERIFIED_MESSAGE],
							resend: [resendError]
						},
						message: RESEND_VERIFICATION_SUCCESS_FALLBACK
					})
				);
			}

			return fail(
				500,
				formState({
					success: false,
					values: { email },
					needsVerification: true,
					errors: {
						form: [ACCOUNT_NOT_VERIFIED_MESSAGE],
						resend: ['Unable to reach the identity service. Please try again later.']
					}
				})
			);
		}
	}
};
