import type { paths } from '$lib/api/generated/profile';
import { apiConfig } from './config';
import { createServerClient } from './client';

/** Creates a typed Profile API client with an optional server-held bearer token. */
export function createProfileApi(token?: string) {
	return createServerClient<paths>(apiConfig.profileBaseUrl, token);
}
