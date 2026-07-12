import { isActionFailure } from '@sveltejs/kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '$lib/server/api/errors';
import { createIdentityApi } from '$lib/server/api/identity';
import { actions, load, type VerifyFormState } from './+page.server';

vi.mock('$lib/server/api/identity', () => ({
	createIdentityApi: vi.fn()
}));

const createIdentityApiMock = vi.mocked(createIdentityApi);
const get = vi.fn();

async function submit(code: string) {
	const action = actions.default;
	if (typeof action !== 'function') throw new Error('default action missing');
	return action({ params: { code } } as never);
}

function expectFailure(result: unknown): { status: number; data: VerifyFormState } {
	expect(isActionFailure(result)).toBe(true);
	if (!isActionFailure(result) || result.data === undefined) {
		throw new Error('expected ActionFailure with data');
	}
	return { status: result.status, data: result.data as VerifyFormState };
}

describe('verify form action', () => {
	beforeEach(() => {
		get.mockReset();
		createIdentityApiMock.mockReset();
		createIdentityApiMock.mockReturnValue({ GET: get } as never);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('exposes the code from the route for the page load', async () => {
		await expect(load({ params: { code: '  abc123  ' } } as never)).resolves.toEqual({
			code: 'abc123',
			hasCode: true
		});
	});

	it('marks empty codes as missing on load', async () => {
		await expect(load({ params: { code: '   ' } } as never)).resolves.toEqual({
			code: '',
			hasCode: false
		});
	});

	it('rejects missing codes before calling Identity', async () => {
		const result = expectFailure(await submit(''));

		expect(result.status).toBe(400);
		expect(result.data).toEqual({
			success: false,
			error: 'Verification code is missing.'
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
	});

	it('rejects whitespace-only codes before calling Identity', async () => {
		const result = expectFailure(await submit('   \t  '));

		expect(result.status).toBe(400);
		expect(result.data).toEqual({
			success: false,
			error: 'Verification code is missing.'
		});
		expect(createIdentityApiMock).not.toHaveBeenCalled();
		expect(get).not.toHaveBeenCalled();
	});

	it('trims the path code before calling Identity', async () => {
		get.mockResolvedValue({
			data: 'Account verified.',
			error: undefined,
			response: new Response()
		});

		const result = await submit('  abc123  ');

		expect(isActionFailure(result)).toBe(false);
		expect(result).toEqual({
			success: true,
			message: 'Account verified.'
		});
		expect(get).toHaveBeenCalledWith('/identities/verify/{verificationCode}', {
			params: {
				path: { verificationCode: 'abc123' }
			}
		});
	});

	it('posts the path code to Identity verify and returns success', async () => {
		get.mockResolvedValue({
			data: 'Account verified.',
			error: undefined,
			response: new Response()
		});

		const result = await submit('code-with-symbols%');

		expect(isActionFailure(result)).toBe(false);
		expect(result).toEqual({
			success: true,
			message: 'Account verified.'
		});
		expect(get).toHaveBeenCalledWith('/identities/verify/{verificationCode}', {
			params: {
				path: { verificationCode: 'code-with-symbols%' }
			}
		});
	});

	it('falls back to the default success message when Identity returns an empty body', async () => {
		get.mockResolvedValue({ data: '', error: undefined, response: new Response() });

		const result = await submit('abc123');

		expect(result).toEqual({
			success: true,
			message: 'Account verified.'
		});
	});

	it('surfaces Identity problem details as a form error', async () => {
		get.mockRejectedValue(
			new ApiError(400, {
				status: 400,
				title: 'One or more errors occurred!',
				detail: 'Invalid verification code.'
			})
		);

		const result = expectFailure(await submit('bad-code'));

		expect(result.status).toBe(400);
		expect(result.data).toEqual({
			success: false,
			error: 'Invalid verification code.'
		});
	});

	it('falls back to ApiError title when detail is missing', async () => {
		get.mockRejectedValue(
			new ApiError(404, {
				status: 404,
				title: 'Verification code not found.'
			})
		);

		const result = expectFailure(await submit('missing-code'));

		expect(result.status).toBe(404);
		expect(result.data).toEqual({
			success: false,
			error: 'Verification code not found.'
		});
	});

	it('clamps out-of-range ApiError status to 400', async () => {
		get.mockRejectedValue(
			new ApiError(0, {
				status: 0,
				detail: 'Upstream returned an invalid status.'
			})
		);

		const result = expectFailure(await submit('abc123'));

		expect(result.status).toBe(400);
		expect(result.data).toEqual({
			success: false,
			error: 'Upstream returned an invalid status.'
		});
	});

	it('uses a safe form error when Identity is unreachable', async () => {
		get.mockRejectedValue(new Error('ECONNREFUSED'));

		const result = expectFailure(await submit('abc123'));

		expect(result.status).toBe(500);
		expect(result.data).toEqual({
			success: false,
			error: 'Unable to reach the identity service. Please try again later.'
		});
	});
});
