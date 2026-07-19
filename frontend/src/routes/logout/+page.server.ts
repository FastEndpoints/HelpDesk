import { redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { clearSessionToken } from '$lib/server/api/session';

export const load: PageServerLoad = async () => {
	redirect(303, '/');
};

export const actions: Actions = {
	default: async ({ cookies }) => {
		clearSessionToken(cookies);
		redirect(303, '/');
	}
};
