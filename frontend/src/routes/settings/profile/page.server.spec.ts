import { isActionFailure, isHttpError, isRedirect } from '@sveltejs/kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import { createProfileApi } from '$lib/server/api/profile';
import { clearSessionToken, readSessionToken } from '$lib/server/api/session';
import { actions, load, type ProfileActionState, type ProfileView } from './+page.server';

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
const put = vi.fn();
const del = vi.fn();
const cookies = {
	get: vi.fn(),
	set: vi.fn(),
	delete: vi.fn()
};

const PROFILE: ProfileView = {
	id: 'profile-1',
	email: 'ada@example.test',
	displayName: 'Ada Lovelace',
	status: 'Active',
	pictureUrl: 'https://profile.test/profile-pictures/ada.png'
};

function requestOf(fields: Record<string, string | File>): Request {
	const body = new FormData();
	for (const [key, value] of Object.entries(fields)) {
		body.set(key, value);
	}
	return new Request('http://frontend.test/settings/profile', { method: 'POST', body });
}

async function runAction(
	name: 'update' | 'uploadPicture' | 'deletePicture',
	fields: Record<string, string | File>
) {
	const action = actions[name];
	if (typeof action !== 'function') throw new Error(`${name} action missing`);
	return action({ request: requestOf(fields), cookies } as never);
}

function expectFailure(result: unknown): { status: number; data: ProfileActionState } {
	expect(isActionFailure(result)).toBe(true);
	if (!isActionFailure(result) || result.data === undefined) {
		throw new Error('expected ActionFailure with data');
	}
	return { status: result.status, data: result.data as ProfileActionState };
}

function expectRedirect(error: unknown, location: string) {
	expect(isRedirect(error)).toBe(true);
	if (!isRedirect(error)) throw error;
	expect(error.status).toBe(303);
	expect(error.location).toBe(location);
}

function pngFile(name = 'avatar.png', size = 12): File {
	return new File([new Uint8Array(size)], name, { type: 'image/png' });
}

describe('profile page load', () => {
	beforeEach(() => {
		get.mockReset();
		put.mockReset();
		del.mockReset();
		createProfileApiMock.mockReset();
		readSessionTokenMock.mockReset();
		clearSessionTokenMock.mockReset();
		createProfileApiMock.mockReturnValue({ GET: get, PUT: put, DELETE: del } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('redirects to login with return URL when no session', async () => {
		readSessionTokenMock.mockReturnValue(undefined);

		try {
			await load({ cookies } as never);
			throw new Error('expected redirect');
		} catch (error) {
			expectRedirect(error, '/login?redirectTo=%2Fsettings%2Fprofile');
		}

		expect(createProfileApiMock).not.toHaveBeenCalled();
	});

	it('loads the current profile', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({ data: PROFILE });

		const data = await load({ cookies } as never);

		expect(createProfileApiMock).toHaveBeenCalledWith('jwt-token');
		expect(get).toHaveBeenCalledWith('/profiles/me');
		expect(data).toEqual({ profile: PROFILE });
	});

	it('normalizes missing pictureUrl to null', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({
			data: { ...PROFILE, pictureUrl: undefined }
		});

		const data = (await load({ cookies } as never)) as { profile: ProfileView };

		expect(data.profile.pictureUrl).toBeNull();
	});

	it('throws 502 when profile payload is incomplete', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockResolvedValue({ data: { id: 'x' } });

		try {
			await load({ cookies } as never);
			throw new Error('expected http error');
		} catch (error) {
			expect(isHttpError(error)).toBe(true);
			if (!isHttpError(error)) throw error;
			expect(error.status).toBe(502);
		}
	});

	it.each([401, 403, 404])('clears session and redirects to login on %s', async (status) => {
		readSessionTokenMock.mockReturnValue('bad-token');
		get.mockRejectedValue(new ApiError(status, { status, title: 'Denied' }));

		try {
			await load({ cookies } as never);
			throw new Error('expected redirect');
		} catch (error) {
			expectRedirect(error, '/login?redirectTo=%2Fsettings%2Fprofile');
		}

		expect(clearSessionTokenMock).toHaveBeenCalledWith(cookies);
	});

	it('throws 503 when Profile is unreachable', async () => {
		readSessionTokenMock.mockReturnValue('jwt-token');
		get.mockRejectedValue(new Error('ECONNREFUSED'));

		try {
			await load({ cookies } as never);
			throw new Error('expected http error');
		} catch (error) {
			expect(isHttpError(error)).toBe(true);
			if (!isHttpError(error)) throw error;
			expect(error.status).toBe(503);
		}

		expect(clearSessionTokenMock).not.toHaveBeenCalled();
	});
});

describe('profile update action', () => {
	beforeEach(() => {
		get.mockReset();
		put.mockReset();
		del.mockReset();
		createProfileApiMock.mockReset();
		readSessionTokenMock.mockReset();
		clearSessionTokenMock.mockReset();
		createProfileApiMock.mockReturnValue({ GET: get, PUT: put, DELETE: del } as never);
		readSessionTokenMock.mockReturnValue('jwt-token');
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('rejects empty display name before calling Profile', async () => {
		const result = expectFailure(await runAction('update', { displayName: '   ' }));

		expect(result.status).toBe(400);
		expect(result.data).toMatchObject({
			success: false,
			action: 'update',
			values: { displayName: '' },
			errors: { displayName: ['Display name is required.'] }
		});
		expect(put).not.toHaveBeenCalled();
	});

	it('rejects oversized display name before calling Profile', async () => {
		const result = expectFailure(await runAction('update', { displayName: 'x'.repeat(101) }));

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			displayName: ['Display name must be at most 100 characters.']
		});
		expect(put).not.toHaveBeenCalled();
	});

	it('trims display name, puts update, and returns success state', async () => {
		put.mockResolvedValue({
			data: { ...PROFILE, displayName: 'Countess Ada' },
			error: undefined,
			response: new Response()
		});

		const result = await runAction('update', { displayName: '  Countess Ada  ' });

		expect(result).toMatchObject({
			success: true,
			action: 'update',
			message: 'Display name updated.',
			values: { displayName: 'Countess Ada' },
			profile: { ...PROFILE, displayName: 'Countess Ada' }
		});
		expect(put).toHaveBeenCalledWith('/profiles/me', {
			body: { displayName: 'Countess Ada' }
		});
	});

	it('maps Profile field errors onto the form', async () => {
		put.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more validation errors occurred.',
				errors: [{ name: 'DisplayName', reason: 'Display name is required.' }]
			})
		);

		const result = expectFailure(await runAction('update', { displayName: 'Ada' }));

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			displayName: ['Display name is required.']
		});
	});

	it('surfaces non-field ApiError details as a form error', async () => {
		put.mockRejectedValue(
			new ApiError(409, {
				status: 409,
				detail: 'Conflict while updating profile.'
			})
		);

		const result = expectFailure(await runAction('update', { displayName: 'Ada' }));

		expect(result.status).toBe(409);
		expect(result.data.errors).toEqual({
			form: ['Conflict while updating profile.']
		});
	});

	it('uses a safe form error when Profile is unreachable', async () => {
		put.mockRejectedValue(new Error('ECONNREFUSED'));

		const result = expectFailure(await runAction('update', { displayName: 'Ada' }));

		expect(result.status).toBe(500);
		expect(result.data.errors).toEqual({
			form: ['Unable to reach the profile service. Please try again later.']
		});
	});

	it('redirects to login when session is missing', async () => {
		readSessionTokenMock.mockReturnValue(undefined);

		try {
			await runAction('update', { displayName: 'Ada' });
			throw new Error('expected redirect');
		} catch (error) {
			expectRedirect(error, '/login?redirectTo=%2Fsettings%2Fprofile');
		}
	});

	it('clears session and redirects on 401 from Profile', async () => {
		put.mockRejectedValue(new ApiError(401, { status: 401, title: 'Unauthorized' }));

		try {
			await runAction('update', { displayName: 'Ada' });
			throw new Error('expected redirect');
		} catch (error) {
			expectRedirect(error, '/login?redirectTo=%2Fsettings%2Fprofile');
		}

		expect(clearSessionTokenMock).toHaveBeenCalledWith(cookies);
	});
});

describe('profile uploadPicture action', () => {
	beforeEach(() => {
		get.mockReset();
		put.mockReset();
		del.mockReset();
		createProfileApiMock.mockReset();
		readSessionTokenMock.mockReset();
		clearSessionTokenMock.mockReset();
		createProfileApiMock.mockReturnValue({ GET: get, PUT: put, DELETE: del } as never);
		readSessionTokenMock.mockReturnValue('jwt-token');
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('rejects missing file before calling Profile', async () => {
		const result = expectFailure(
			await runAction('uploadPicture', { displayName: PROFILE.displayName })
		);

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			file: ['A picture file is required.']
		});
		expect(put).not.toHaveBeenCalled();
	});

	it('rejects disallowed file types before calling Profile', async () => {
		const result = expectFailure(
			await runAction('uploadPicture', {
				displayName: PROFILE.displayName,
				file: new File([new Uint8Array(8)], 'avatar.gif', { type: 'image/gif' })
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			file: ['Only PNG and JPG images are allowed.']
		});
		expect(put).not.toHaveBeenCalled();
	});

	it('rejects oversized files before calling Profile', async () => {
		const result = expectFailure(
			await runAction('uploadPicture', {
				displayName: PROFILE.displayName,
				file: pngFile('big.png', 5 * 1024 * 1024 + 1)
			})
		);

		expect(result.status).toBe(400);
		expect(result.data.errors).toEqual({
			file: ['Picture exceeds the configured upload limit.']
		});
		expect(put).not.toHaveBeenCalled();
	});

	it('uploads multipart file and returns success state', async () => {
		const file = pngFile();
		put.mockResolvedValue({
			data: PROFILE,
			error: undefined,
			response: new Response()
		});

		const result = await runAction('uploadPicture', {
			displayName: PROFILE.displayName,
			file
		});

		expect(result).toMatchObject({
			success: true,
			action: 'uploadPicture',
			message: 'Profile picture updated.',
			profile: PROFILE
		});
		expect(put).toHaveBeenCalledTimes(1);
		const [, options] = put.mock.calls[0] as [string, { body: FormData; bodySerializer: unknown }];
		expect(options.body).toBeInstanceOf(FormData);
		expect(options.body.get('file')).toBeInstanceOf(File);
		expect(typeof options.bodySerializer).toBe('function');
	});

	it('maps file field errors from Profile', async () => {
		put.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				errors: [{ name: 'File', reason: 'Only PNG and JPG images are allowed.' }]
			})
		);

		const result = expectFailure(
			await runAction('uploadPicture', {
				displayName: PROFILE.displayName,
				file: pngFile()
			})
		);

		expect(result.data.errors).toEqual({
			file: ['Only PNG and JPG images are allowed.']
		});
	});
});

describe('profile deletePicture action', () => {
	beforeEach(() => {
		get.mockReset();
		put.mockReset();
		del.mockReset();
		createProfileApiMock.mockReset();
		readSessionTokenMock.mockReset();
		clearSessionTokenMock.mockReset();
		createProfileApiMock.mockReturnValue({ GET: get, PUT: put, DELETE: del } as never);
		readSessionTokenMock.mockReturnValue('jwt-token');
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('deletes the picture and returns success state', async () => {
		del.mockResolvedValue({
			data: { ...PROFILE, pictureUrl: null },
			error: undefined,
			response: new Response()
		});

		const result = await runAction('deletePicture', {
			displayName: PROFILE.displayName
		});

		expect(result).toMatchObject({
			success: true,
			action: 'deletePicture',
			message: 'Profile picture removed.',
			profile: { ...PROFILE, pictureUrl: null }
		});
		expect(del).toHaveBeenCalledWith('/profiles/me/picture');
	});

	it('uses a safe form error when Profile is unreachable', async () => {
		del.mockRejectedValue(new Error('ECONNREFUSED'));

		const result = expectFailure(
			await runAction('deletePicture', { displayName: PROFILE.displayName })
		);

		expect(result.status).toBe(500);
		expect(result.data.errors).toEqual({
			form: ['Unable to reach the profile service. Please try again later.']
		});
	});

	it('clears session and redirects on 403 from Profile', async () => {
		del.mockRejectedValue(new ApiError(403, { status: 403, title: 'Forbidden' }));

		try {
			await runAction('deletePicture', { displayName: PROFILE.displayName });
			throw new Error('expected redirect');
		} catch (error) {
			expectRedirect(error, '/login?redirectTo=%2Fsettings%2Fprofile');
		}

		expect(clearSessionTokenMock).toHaveBeenCalledWith(cookies);
	});
});
