import { env } from '$env/dynamic/private';

function requiredUrl(name: 'IDENTITY_API_BASE_URL' | 'PROFILE_API_BASE_URL'): string {
	const value = env[name];
	if (!value) throw new Error(`${name} is required`);

	const url = new URL(value);
	if (url.protocol !== 'http:' && url.protocol !== 'https:') {
		throw new Error(`${name} must use HTTP or HTTPS`);
	}
	return url.toString().replace(/\/$/, '');
}

export const apiConfig = {
	get identityBaseUrl() {
		return requiredUrl('IDENTITY_API_BASE_URL');
	},
	get profileBaseUrl() {
		return requiredUrl('PROFILE_API_BASE_URL');
	}
};
