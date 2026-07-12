import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createIdentityApi } from '$lib/server/api/identity';
import { ApiError } from '$lib/server/api/errors';
import { mapProblemFieldErrors } from '$lib/server/api/problem';
import { writeSessionToken } from '$lib/server/api/session';

export type LoginFieldErrors = {
	email?: string[];
	password?: string[];
	form?: string[];
};

export type LoginFormState = {
	success: boolean;
	message?: string;
	values: {
		email: string;
	};
	errors: LoginFieldErrors;
};

function formState(
	partial: Omit<LoginFormState, 'values' | 'errors'> &
		Partial<Pick<LoginFormState, 'values' | 'errors'>> & { values?: { email?: string } }
): LoginFormState {
	return {
		success: partial.success,
		message: partial.message,
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

export const load: PageServerLoad = async () => {
	return {};
};

export const actions: Actions = {
	default: async ({ request, cookies }) => {
		const formData = await request.formData();
		const email = String(formData.get('email') ?? '').trim();
		const password = String(formData.get('password') ?? '');

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

				if (!mapped.email && !mapped.password) {
					mapped.form = [
						error.problem.detail ?? error.problem.title ?? 'Sign-in failed. Please try again.'
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

		writeSessionToken(cookies, accessToken, maxAge);
		redirect(303, '/');
	}
};
