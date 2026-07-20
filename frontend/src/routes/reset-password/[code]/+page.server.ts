import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { ApiError } from '$lib/server/api/errors';
import { mapProblemFieldErrors } from '$lib/server/api/problem';
import {
	postResetPassword,
	RESET_PASSWORD_SUCCESS_FALLBACK,
	validateResetPassword
} from '$lib/server/api/password-reset';

export type ResetPasswordFieldErrors = {
	password?: string[];
	confirmPassword?: string[];
	form?: string[];
};

export type ResetPasswordFormState = {
	success: boolean;
	message?: string;
	errors: ResetPasswordFieldErrors;
};

function formState(
	partial: Omit<ResetPasswordFormState, 'errors'> & Partial<Pick<ResetPasswordFormState, 'errors'>>
): ResetPasswordFormState {
	return {
		success: partial.success,
		message: partial.message,
		errors: partial.errors ?? {}
	};
}

export const load: PageServerLoad = async ({ params }) => {
	const code = params.code?.trim() ?? '';
	return {
		code,
		hasCode: code.length > 0
	};
};

export const actions: Actions = {
	reset: async ({ request, params }) => {
		const code = params.code?.trim() ?? '';
		const formData = await request.formData();
		const password = String(formData.get('password') ?? '');
		const confirmPassword = String(formData.get('confirmPassword') ?? '');

		if (!code) {
			return fail(
				400,
				formState({
					success: false,
					errors: { form: ['Reset code is missing.'] }
				})
			);
		}

		const errors: ResetPasswordFieldErrors = {};
		const passwordError = validateResetPassword(password);
		if (passwordError) errors.password = [passwordError];

		if (!confirmPassword) {
			errors.confirmPassword = ['Confirm your password.'];
		} else if (password !== confirmPassword) {
			errors.confirmPassword = ['Passwords do not match.'];
		}

		if (Object.keys(errors).length > 0) {
			return fail(400, formState({ success: false, errors }));
		}

		try {
			const message = await postResetPassword(code, password);

			return formState({
				success: true,
				message
			});
		} catch (error) {
			if (error instanceof ApiError) {
				const fieldErrors = mapProblemFieldErrors(error.problem);
				const mapped: ResetPasswordFieldErrors = {};

				if (fieldErrors.password?.length) mapped.password = fieldErrors.password;

				if (!mapped.password) {
					mapped.form = [
						error.problem.detail ??
							error.problem.title ??
							'Password reset failed. Please try again.'
					];
				}

				return fail(
					error.status >= 400 && error.status < 600 ? error.status : 400,
					formState({
						success: false,
						errors: mapped,
						message: RESET_PASSWORD_SUCCESS_FALLBACK
					})
				);
			}

			return fail(
				500,
				formState({
					success: false,
					errors: {
						form: ['Unable to reach the identity service. Please try again later.']
					}
				})
			);
		}
	}
};
