import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createIdentityApi } from '$lib/server/api/identity';
import { ApiError } from '$lib/server/api/errors';
import { mapProblemFieldErrors } from '$lib/server/api/problem';
import {
	postResendVerification,
	RESEND_VERIFICATION_SUCCESS_FALLBACK,
	validateResendEmail
} from '$lib/server/api/resend-verification';

export type RegisterFieldErrors = {
	email?: string[];
	password?: string[];
	confirmPassword?: string[];
	form?: string[];
	resend?: string[];
};

export type RegisterFormState = {
	success: boolean;
	message?: string;
	resendSuccess?: boolean;
	values: {
		email: string;
	};
	errors: RegisterFieldErrors;
};

function formState(
	partial: Omit<RegisterFormState, 'values' | 'errors'> &
		Partial<Pick<RegisterFormState, 'values' | 'errors' | 'resendSuccess'>> & {
			values?: { email?: string };
		}
): RegisterFormState {
	return {
		success: partial.success,
		message: partial.message,
		resendSuccess: partial.resendSuccess,
		values: { email: partial.values?.email ?? '' },
		errors: partial.errors ?? {}
	};
}

export const load: PageServerLoad = async () => {
	return {};
};

export const actions: Actions = {
	register: async ({ request }) => {
		const formData = await request.formData();
		const email = String(formData.get('email') ?? '').trim();
		const password = String(formData.get('password') ?? '');
		const confirmPassword = String(formData.get('confirmPassword') ?? '');

		const errors: RegisterFieldErrors = {};

		if (!email) {
			errors.email = ['Email is required.'];
		} else if (email.length > 320) {
			errors.email = ['Email must be at most 320 characters.'];
		} else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
			errors.email = ['Email must be a valid email address.'];
		}

		if (!password) {
			errors.password = ['Password is required.'];
		} else if (password.length < 12) {
			errors.password = ['Password must be at least 12 characters.'];
		} else if (password.length > 128) {
			errors.password = ['Password must be at most 128 characters.'];
		}

		if (!confirmPassword) {
			errors.confirmPassword = ['Confirm your password.'];
		} else if (password !== confirmPassword) {
			errors.confirmPassword = ['Passwords do not match.'];
		}

		if (Object.keys(errors).length > 0) {
			return fail(400, formState({ success: false, values: { email }, errors }));
		}

		try {
			const { data } = await createIdentityApi().POST('/identities/register', {
				body: { email, password }
			});

			return formState({
				success: true,
				message:
					typeof data === 'string' && data.length > 0
						? data
						: 'Signup successful. Please check your email for a verification link.',
				values: { email }
			});
		} catch (error) {
			if (error instanceof ApiError) {
				const fieldErrors = mapProblemFieldErrors(error.problem);
				const mapped: RegisterFieldErrors = {};

				if (fieldErrors.email?.length) mapped.email = fieldErrors.email;
				if (fieldErrors.password?.length) mapped.password = fieldErrors.password;

				if (!mapped.email && !mapped.password) {
					mapped.form = [
						error.problem.detail ?? error.problem.title ?? 'Registration failed. Please try again.'
					];
				}

				return fail(
					error.status >= 400 && error.status < 600 ? error.status : 400,
					formState({
						success: false,
						values: { email },
						errors: mapped,
						message: error.problem.detail ?? error.problem.title
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
	},

	resend: async ({ request }) => {
		const formData = await request.formData();
		const email = String(formData.get('email') ?? '').trim();
		const emailError = validateResendEmail(email);

		if (emailError) {
			return fail(
				400,
				formState({
					success: true,
					values: { email },
					errors: { resend: [emailError] }
				})
			);
		}

		try {
			const message = await postResendVerification(email);

			return formState({
				success: true,
				resendSuccess: true,
				message,
				values: { email }
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
						success: true,
						values: { email },
						errors: { resend: [resendError] },
						message: RESEND_VERIFICATION_SUCCESS_FALLBACK
					})
				);
			}

			return fail(
				500,
				formState({
					success: true,
					values: { email },
					errors: {
						resend: ['Unable to reach the identity service. Please try again later.']
					}
				})
			);
		}
	}
};
