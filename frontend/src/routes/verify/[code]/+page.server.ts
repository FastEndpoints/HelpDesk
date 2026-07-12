import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createIdentityApi } from '$lib/server/api/identity';
import { ApiError } from '$lib/server/api/errors';

export type VerifyFormState = {
	success: boolean;
	message?: string;
	error?: string;
};

export const load: PageServerLoad = async ({ params }) => {
	const code = params.code?.trim() ?? '';
	return {
		code,
		hasCode: code.length > 0
	};
};

export const actions: Actions = {
	default: async ({ params }) => {
		const code = params.code?.trim() ?? '';

		if (!code) {
			return fail(400, {
				success: false,
				error: 'Verification code is missing.'
			} satisfies VerifyFormState);
		}

		try {
			const { data } = await createIdentityApi().GET('/identities/verify/{verificationCode}', {
				params: {
					path: { verificationCode: code }
				}
			});

			return {
				success: true,
				message:
					typeof data === 'string' && data.length > 0 ? data : 'Account verified.'
			} satisfies VerifyFormState;
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(
					error.status >= 400 && error.status < 600 ? error.status : 400,
					{
						success: false,
						error:
							error.problem.detail ??
							error.problem.title ??
							'Verification failed. Please try again.'
					} satisfies VerifyFormState
				);
			}

			return fail(500, {
				success: false,
				error: 'Unable to reach the identity service. Please try again later.'
			} satisfies VerifyFormState);
		}
	}
};
