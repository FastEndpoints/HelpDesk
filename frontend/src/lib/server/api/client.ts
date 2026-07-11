import createClient from 'openapi-fetch';
import { toApiError } from './errors';

export function createServerClient<T extends object>(baseUrl: string, token?: string) {
	const client = createClient<T>({
		baseUrl,
		headers: token ? { Authorization: `Bearer ${token}` } : undefined
	});

	client.use({
		async onResponse({ response }) {
			if (!response.ok) throw await toApiError(response);
		}
	});

	return client;
}
