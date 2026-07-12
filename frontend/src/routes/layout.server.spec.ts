import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import { createProfileApi } from '$lib/server/api/profile';
import { clearSessionToken, readSessionToken } from '$lib/server/api/session';
import { load } from './+layout.server';

vi.mock('$lib/server/api/profile', () => ({
	createProfileApi: vi.fn()
}));

vi.mock('$lib/server/api/session', () => ({
	readSessionToken: vi.fn(),
	clearSessionToken: vi.fn()
}));

const createProfileApiMock = vi.mocked(createProfileApi);
const readSessionTokenMock = vi.mocked(readSessionToken);
const clearSessionTokenMock = vi.mocked(clearSessionToken);
const get = vi.fn();
const cookies = {
	get: vi.fn(),
	set: vi.fn(),
	delete: vi.fn()
};

describe('root layout load', () => {
	beforeEach(() => {
		get.mockReset();
		createProfileApiMock.mockReset();
		readSessionTokenMock.mockReset();
		clearSessionTokenMock.mockReset();
		createProfileApiMock.mockReturnValue({ GET: get } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('returns null user when no session cookie', async () => {
		readSessionTokenMock.mockReturnValue(undefined);

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(createProfileApiMock).not.toHaveBeenCalled();
		expect(get).not.toHaveBeenCalled();
	});

	it('loads display name and picture from Profile GET /profiles/me', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({
			data: {
				displayName: 'Ada Lovelace',
				pictureUrl: 'https://profile.test/profile-pictures/ada.png',
				email: 'ada@example.test',
				id: 'profile-1',
				status: 'Active'
			}
		});

		const data = await load({ cookies } as never);

		expect(createProfileApiMock).toHaveBeenCalledWith('jwt-token');
		expect(get).toHaveBeenCalledWith('/profiles/me');
		expect(data).toEqual({
			user: {
				displayName: 'Ada Lovelace',
				pictureUrl: 'https://profile.test/profile-pictures/ada.png'
			}
		});
		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});

	it('normalizes missing pictureUrl to null', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({
			data: {
				displayName: 'No Pic',
				pictureUrl: null
			}
		});

		const data = await load({ cookies } as never);

		expect(data).toEqual({
			user: {
				displayName: 'No Pic',
				pictureUrl: null
			}
		});
	});

	it('normalizes undefined pictureUrl to null', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({
			data: {
				displayName: 'No Pic Field'
			}
		});

		const data = await load({ cookies } as never);

		expect(data).toEqual({
			user: {
				displayName: 'No Pic Field',
				pictureUrl: null
			}
		});
	});

	it('returns null user when profile payload lacks displayName', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({ data: { id: 'x' } });

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});

	it('returns null user when displayName is empty', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({ data: { displayName: '', pictureUrl: null } });

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});

	it('returns null user when Profile returns no data body', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({ data: undefined });

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});

	it.each([401, 403, 404])('clears session and returns null user on %s', async (status) => {
		readSessionTokenMock.mockReturnValue('bad-token');
		get.mockRejectedValue(
			new ApiError(status, { status, title: 'Denied', detail: 'nope' })
		);

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(clearSessionTokenMock).toHaveBeenCalledWith(cookies);
	});

	it('returns null user without clearing session on unreachable Profile service', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockRejectedValue(new Error('ECONNREFUSED'));

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});

	it('returns null user without clearing session on non-auth ApiError', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockRejectedValue(new ApiError(500, { status: 500, title: 'Server error' }));

		const data = await load({ cookies } as never);

		expect(data).toEqual({ user: null });
		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});
});
