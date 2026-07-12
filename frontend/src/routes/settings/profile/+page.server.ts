import { error, fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { ApiError } from '$lib/server/api/errors';
import { createProfileApi } from '$lib/server/api/profile';
import { mapProblemFieldErrors } from '$lib/server/api/problem';
import { clearSessionToken, readSessionToken } from '$lib/server/api/session';

const PROFILE_PATH = '/settings/profile';
const LOGIN_REDIRECT = `/login?redirectTo=${encodeURIComponent(PROFILE_PATH)}`;
const DISPLAY_NAME_MAX = 100;
const MAX_UPLOAD_BYTES = 5 * 1024 * 1024;
const ALLOWED_CONTENT_TYPES = new Set(['image/jpeg', 'image/jpg', 'image/png']);
const ALLOWED_EXTENSIONS = new Set(['.jpg', '.jpeg', '.png']);

export type ProfileView = {
	id: string;
	email: string;
	displayName: string;
	status: string;
	pictureUrl: string | null;
};

export type ProfileFieldErrors = {
	displayName?: string[];
	file?: string[];
	form?: string[];
};

export type ProfileActionState = {
	success: boolean;
	action?: 'update' | 'uploadPicture' | 'deletePicture';
	message?: string;
	values: {
		displayName: string;
	};
	errors: ProfileFieldErrors;
	profile?: ProfileView;
};

function actionState(
	partial: Omit<ProfileActionState, 'values' | 'errors'> &
		Partial<Pick<ProfileActionState, 'values' | 'errors' | 'profile'>> & {
			values?: { displayName?: string };
		}
): ProfileActionState {
	return {
		success: partial.success,
		action: partial.action,
		message: partial.message,
		values: { displayName: partial.values?.displayName ?? '' },
		errors: partial.errors ?? {},
		profile: partial.profile
	};
}

function mapProfile(data: {
	id?: string;
	email?: string;
	displayName?: string;
	status?: string;
	pictureUrl?: string | null;
}): ProfileView | null {
	if (!data.displayName || !data.email || !data.id || !data.status) {
		return null;
	}

	return {
		id: data.id,
		email: data.email,
		displayName: data.displayName,
		status: data.status,
		pictureUrl: data.pictureUrl ?? null
	};
}

function requireToken(cookies: Parameters<PageServerLoad>[0]['cookies']): string {
	const token = readSessionToken(cookies);
	if (!token) {
		redirect(303, LOGIN_REDIRECT);
	}
	return token;
}

function handleAuthFailure(
	err: unknown,
	cookies: Parameters<PageServerLoad>[0]['cookies']
): void {
	if (
		err instanceof ApiError &&
		(err.status === 401 || err.status === 403 || err.status === 404)
	) {
		clearSessionToken(cookies);
		redirect(303, LOGIN_REDIRECT);
	}
}

function mapApiFormErrors(error: ApiError, values: { displayName: string }): ProfileActionState {
	const fieldErrors = mapProblemFieldErrors(error.problem);
	const mapped: ProfileFieldErrors = {};

	if (fieldErrors.displayName?.length) mapped.displayName = fieldErrors.displayName;
	if (fieldErrors.file?.length) mapped.file = fieldErrors.file;

	if (!mapped.displayName && !mapped.file) {
		mapped.form = [
			error.problem.detail ?? error.problem.title ?? 'Profile update failed. Please try again.'
		];
	}

	return actionState({
		success: false,
		values,
		errors: mapped,
		message: error.problem.detail ?? error.problem.title
	});
}

function isAllowedPicture(file: File): boolean {
	const contentType = file.type.split(';', 2)[0]?.trim().toLowerCase() ?? '';
	const contentTypeOk = contentType.length > 0 && ALLOWED_CONTENT_TYPES.has(contentType);
	const extension = file.name.includes('.')
		? `.${file.name.split('.').pop()!.toLowerCase()}`
		: '';
	const extensionOk = extension.length > 0 && ALLOWED_EXTENSIONS.has(extension);
	return contentTypeOk || extensionOk;
}

export const load: PageServerLoad = async ({ cookies }) => {
	const token = requireToken(cookies);

	try {
		const { data } = await createProfileApi(token).GET('/profiles/me');
		const profile = data ? mapProfile(data) : null;

		if (!profile) {
			error(502, 'Profile service returned an incomplete profile.');
		}

		return { profile };
	} catch (err) {
		handleAuthFailure(err, cookies);

		if (err && typeof err === 'object' && 'status' in err) {
			throw err;
		}

		error(503, 'Unable to reach the profile service. Please try again later.');
	}
};

export const actions: Actions = {
	update: async ({ request, cookies }) => {
		const token = requireToken(cookies);
		const formData = await request.formData();
		const displayName = String(formData.get('displayName') ?? '').trim();
		const errors: ProfileFieldErrors = {};

		if (!displayName) {
			errors.displayName = ['Display name is required.'];
		} else if (displayName.length > DISPLAY_NAME_MAX) {
			errors.displayName = [`Display name must be at most ${DISPLAY_NAME_MAX} characters.`];
		}

		if (Object.keys(errors).length > 0) {
			return fail(400, actionState({ success: false, action: 'update', values: { displayName }, errors }));
		}

		try {
			const { data } = await createProfileApi(token).PUT('/profiles/me', {
				body: { displayName }
			});
			const profile = data ? mapProfile(data) : null;

			if (!profile) {
				return fail(
					502,
					actionState({
						success: false,
						action: 'update',
						values: { displayName },
						errors: {
							form: ['Profile was updated but the response was incomplete. Please reload.']
						}
					})
				);
			}

			return actionState({
				success: true,
				action: 'update',
				message: 'Display name updated.',
				values: { displayName: profile.displayName },
				profile
			});
		} catch (err) {
			handleAuthFailure(err, cookies);

			if (err instanceof ApiError) {
				return fail(
					err.status >= 400 && err.status < 600 ? err.status : 400,
					mapApiFormErrors(err, { displayName })
				);
			}

			return fail(
				500,
				actionState({
					success: false,
					action: 'update',
					values: { displayName },
					errors: {
						form: ['Unable to reach the profile service. Please try again later.']
					}
				})
			);
		}
	},

	uploadPicture: async ({ request, cookies }) => {
		const token = requireToken(cookies);
		const formData = await request.formData();
		const displayName = String(formData.get('displayName') ?? '').trim();
		const fileEntry = formData.get('file');
		const errors: ProfileFieldErrors = {};

		if (!(fileEntry instanceof File) || fileEntry.size === 0) {
			errors.file = ['A picture file is required.'];
		} else if (fileEntry.size > MAX_UPLOAD_BYTES) {
			errors.file = ['Picture exceeds the configured upload limit.'];
		} else if (!isAllowedPicture(fileEntry)) {
			errors.file = ['Only PNG and JPG images are allowed.'];
		}

		if (Object.keys(errors).length > 0) {
			return fail(
				400,
				actionState({
					success: false,
					action: 'uploadPicture',
					values: { displayName },
					errors
				})
			);
		}

		const file = fileEntry as File;

		try {
			const body = new FormData();
			body.set('file', file, file.name);

			const { data } = await createProfileApi(token).PUT('/profiles/me/picture', {
				// openapi-fetch types model binary as string; FormData is the correct runtime body.
				body: body as unknown as { file: string },
				bodySerializer: () => body
			});
			const profile = data ? mapProfile(data) : null;

			if (!profile) {
				return fail(
					502,
					actionState({
						success: false,
						action: 'uploadPicture',
						values: { displayName },
						errors: {
							form: ['Picture was uploaded but the response was incomplete. Please reload.']
						}
					})
				);
			}

			return actionState({
				success: true,
				action: 'uploadPicture',
				message: 'Profile picture updated.',
				values: { displayName: profile.displayName },
				profile
			});
		} catch (err) {
			handleAuthFailure(err, cookies);

			if (err instanceof ApiError) {
				return fail(
					err.status >= 400 && err.status < 600 ? err.status : 400,
					mapApiFormErrors(err, { displayName })
				);
			}

			return fail(
				500,
				actionState({
					success: false,
					action: 'uploadPicture',
					values: { displayName },
					errors: {
						form: ['Unable to reach the profile service. Please try again later.']
					}
				})
			);
		}
	},

	deletePicture: async ({ request, cookies }) => {
		const token = requireToken(cookies);
		const formData = await request.formData();
		const displayName = String(formData.get('displayName') ?? '').trim();

		try {
			const { data } = await createProfileApi(token).DELETE('/profiles/me/picture');
			const profile = data ? mapProfile(data) : null;

			if (!profile) {
				return fail(
					502,
					actionState({
						success: false,
						action: 'deletePicture',
						values: { displayName },
						errors: {
							form: ['Picture was removed but the response was incomplete. Please reload.']
						}
					})
				);
			}

			return actionState({
				success: true,
				action: 'deletePicture',
				message: 'Profile picture removed.',
				values: { displayName: profile.displayName },
				profile
			});
		} catch (err) {
			handleAuthFailure(err, cookies);

			if (err instanceof ApiError) {
				return fail(
					err.status >= 400 && err.status < 600 ? err.status : 400,
					mapApiFormErrors(err, { displayName })
				);
			}

			return fail(
				500,
				actionState({
					success: false,
					action: 'deletePicture',
					values: { displayName },
					errors: {
						form: ['Unable to reach the profile service. Please try again later.']
					}
				})
			);
		}
	}
};
