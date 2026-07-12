import { describe, expect, it } from 'vitest';
import { mapProblemFieldErrors } from './problem';

describe('mapProblemFieldErrors', () => {
	it('maps FastEndpoints error arrays by field name', () => {
		const mapped = mapProblemFieldErrors({
			status: 400,
			errors: [
				{ name: 'Email', reason: 'Email address is in use!' },
				{ name: 'Password', reason: 'The length of Password must be at least 12 characters.' }
			]
		});

		expect(mapped).toEqual({
			email: ['Email address is in use!'],
			password: ['The length of Password must be at least 12 characters.']
		});
	});

	it('maps record-shaped error bags', () => {
		const mapped = mapProblemFieldErrors({
			status: 400,
			errors: {
				email: ['Email is already registered.']
			}
		});

		expect(mapped).toEqual({
			email: ['Email is already registered.']
		});
	});

	it('returns an empty object when no field errors exist', () => {
		expect(mapProblemFieldErrors({ status: 500, title: 'Server error' })).toEqual({});
	});
});
