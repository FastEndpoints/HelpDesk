import { isRedirect } from '@sveltejs/kit';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { clearSessionToken } from '$lib/server/api/session';
import { actions, load } from './+page.server';

vi.mock('$lib/server/api/session', () => ({
	clearSessionToken: vi.fn()
}));

const clearSessionTokenMock = vi.mocked(clearSessionToken);

describe('logout page server', () => {
	beforeEach(() => {
		clearSessionTokenMock.mockReset();
	});

	it('GET redirects home without clearing the session', async () => {
		try {
			await load({} as never);
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
			if (!isRedirect(error)) throw error;
			expect(error.status).toBe(303);
			expect(error.location).toBe('/');
		}

		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});

	it('POST clears the session cookie and redirects home', async () => {
		const cookies = {} as never;

		try {
			await actions.default!({ cookies } as never);
			throw new Error('expected redirect');
		} catch (error) {
			expect(isRedirect(error)).toBe(true);
			if (!isRedirect(error)) throw error;
			expect(error.status).toBe(303);
			expect(error.location).toBe('/');
		}

		expect(clearSessionTokenMock).toHaveBeenCalledWith(cookies);
	});
});
