import type { paths } from '$lib/api/generated/identity';
import { apiConfig } from './config';
import { createServerClient } from './client';

/** Creates a typed Identity API client for use only in server actions/routes. */
export function createIdentityApi() {
	return createServerClient<paths>(apiConfig.identityBaseUrl);
}
