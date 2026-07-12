import type { LayoutServerLoad } from './$types';
import { ApiError } from '$lib/server/api/errors';
import { createProfileApi } from '$lib/server/api/profile';
import { clearSessionToken, readSessionToken } from '$lib/server/api/session';

export type LayoutUser = {
	displayName: string;
	pictureUrl: string | null;
};

export type LayoutData = {
	user: LayoutUser | null;
};

export const load: LayoutServerLoad = async ({ cookies }): Promise<LayoutData> => {
	const token = readSessionToken(cookies);
	if (!token) {
		return { user: null };
	}

	try {
		const { data } = await createProfileApi(token).GET('/profiles/me');

		if (!data?.displayName) {
			return { user: null };
		}

		return {
			user: {
				displayName: data.displayName,
				pictureUrl: data.pictureUrl ?? null
			}
		};
	} catch (error) {
		if (
			error instanceof ApiError &&
			(error.status === 401 || error.status === 403 || error.status === 404)
		) {
			clearSessionToken(cookies);
		}

		return { user: null };
	}
};
