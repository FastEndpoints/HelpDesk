import { describe, expect, it, vi } from 'vitest';

vi.mock('$lib/server/api/config', () => ({
	apiConfig: { profileBaseUrl: 'http://profile.test' }
}));

import { GET } from './+server';

describe('profile picture proxy', () => {
	it('proxies encoded paths and cache/range headers', async () => {
		expect.assertions(6);
		const fetchMock = vi.fn().mockResolvedValue(
			new Response(new Uint8Array([1, 2]), {
				status: 206,
				headers: {
					'content-type': 'image/jpeg',
					'content-range': 'bytes 0-1/10',
					'accept-ranges': 'bytes'
				}
			})
		);
		const request = new Request('https://helpdesk.test/profile-pictures/a%20b.jpg', {
			headers: { range: 'bytes=0-1', 'if-range': 'picture-etag' }
		});

		const response = (await GET({
			fetch: fetchMock,
			params: { path: 'a b.jpg' },
			request
		} as never)) as Response;

		expect(fetchMock).toHaveBeenCalledOnce();
		const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
		expect(url).toBe('http://profile.test/profile-pictures/a%20b.jpg');
		expect(new Headers(init.headers).get('range')).toBe('bytes=0-1');
		expect(new Headers(init.headers).get('if-range')).toBe('picture-etag');
		expect(response.status).toBe(206);
		expect(response.headers.get('content-range')).toBe('bytes 0-1/10');
	});

	it('preserves not-modified responses', async () => {
		expect.assertions(2);
		const fetchMock = vi
			.fn()
			.mockResolvedValue(new Response(null, { status: 304, headers: { etag: 'picture-etag' } }));

		const response = (await GET({
			fetch: fetchMock,
			params: { path: 'avatar.jpg' },
			request: new Request('https://helpdesk.test/profile-pictures/avatar.jpg')
		} as never)) as Response;

		expect(response.status).toBe(304);
		expect(response.headers.get('etag')).toBe('picture-etag');
	});

	it('preserves unsatisfied-range metadata', async () => {
		expect.assertions(2);
		const fetchMock = vi
			.fn()
			.mockResolvedValue(
				new Response(null, { status: 416, headers: { 'content-range': 'bytes */10' } })
			);

		const response = (await GET({
			fetch: fetchMock,
			params: { path: 'avatar.jpg' },
			request: new Request('https://helpdesk.test/profile-pictures/avatar.jpg', {
				headers: { range: 'bytes=20-30' }
			})
		} as never)) as Response;

		expect(response.status).toBe(416);
		expect(response.headers.get('content-range')).toBe('bytes */10');
	});
});
