import { apiConfig } from '$lib/server/api/config';
import { error } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch, params, request }) => {
	const path = params.path;
	if (!path) error(404, 'Profile picture not found');

	const segments = path.split('/');
	if (segments.some((segment) => !segment || segment === '.' || segment === '..')) {
		error(400, 'Invalid profile picture path');
	}

	const requestHeaders = new Headers();
	for (const name of ['if-none-match', 'if-modified-since', 'range', 'if-range']) {
		const value = request.headers.get(name);
		if (value) requestHeaders.set(name, value);
	}

	const upstream = await fetch(
		`${apiConfig.profileBaseUrl}/profile-pictures/${segments.map(encodeURIComponent).join('/')}`,
		{ headers: requestHeaders }
	);

	const responseHeaders = new Headers();
	for (const name of [
		'content-type',
		'content-length',
		'cache-control',
		'etag',
		'last-modified',
		'accept-ranges',
		'content-range'
	]) {
		const value = upstream.headers.get(name);
		if (value) responseHeaders.set(name, value);
	}

	if (upstream.status === 304) {
		return new Response(null, { status: 304, headers: responseHeaders });
	}
	if (upstream.status === 416) {
		return new Response(upstream.body, { status: 416, headers: responseHeaders });
	}
	if (!upstream.ok) error(upstream.status, 'Profile picture not found');

	return new Response(upstream.body, { status: upstream.status, headers: responseHeaders });
};
