import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { ApiError } from '$lib/server/api/errors';
import { mapProblemFieldErrors } from '$lib/server/api/problem';
import {
	FORGOT_PASSWORD_SUCCESS_FALLBACK,
	postForgotPassword,
	validateForgotEmail
} from '$lib/server/api/password-reset';
import { RESEND_COOLDOWN_MS } from '$lib/resend-cooldown';

export type ForgotPasswordFieldErrors = {
	email?: string[];
	form?: string[];
	resend?: string[];
};

export type ForgotPasswordFormState = {
	success: boolean;
	message?: string;
	/** Remaining reset-request cooldown from Identity (`Reset-Available-In`), when known. */
	resetAvailableInSeconds?: number;
	values: {
		email: string;
	};
	errors: ForgotPasswordFieldErrors;
};

function formState(
	partial: Omit<ForgotPasswordFormState, 'values' | 'errors'> &
		Partial<Pick<ForgotPasswordFormState, 'values' | 'errors'>> & {
			values?: { email?: string };
		}
): ForgotPasswordFormState {
	return {
		success: partial.success,
		message: partial.message,
		resetAvailableInSeconds: partial.resetAvailableInSeconds,
		values: { email: partial.values?.email ?? '' },
		errors: partial.errors ?? {}
	};
}

export const load: PageServerLoad = async () => {
	return {};
};

export const actions: Actions = {
	request: async ({ request }) => {
		const formData = await request.formData();
		const email = String(formData.get('email') ?? '').trim();
		const emailError = validateForgotEmail(email);

		if (emailError) {
			return fail(
				400,
				formState({
					success: false,
					values: { email },
					errors: { email: [emailError] }
				})
			);
		}

		try {
			const { message, resetAvailableInSeconds } = await postForgotPassword(email);

			return formState({
				success: true,
				message,
				resetAvailableInSeconds: resetAvailableInSeconds ?? Math.ceil(RESEND_COOLDOWN_MS / 1000),
				values: { email }
			});
		} catch (error) {
			if (error instanceof ApiError) {
				const fieldErrors = mapProblemFieldErrors(error.problem);
				const mapped: ForgotPasswordFieldErrors = {};

				if (fieldErrors.email?.length) mapped.email = fieldErrors.email;

				if (!mapped.email) {
					mapped.form = [
						error.problem.detail ??
							error.problem.title ??
							'Unable to send reset email. Please try again.'
					];
				}

				return fail(
					error.status >= 400 && error.status < 600 ? error.status : 400,
					formState({
						success: false,
						values: { email },
						errors: mapped,
						message: FORGOT_PASSWORD_SUCCESS_FALLBACK
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
	}
};
