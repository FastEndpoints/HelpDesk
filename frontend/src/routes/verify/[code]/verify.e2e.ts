import { expect, test } from '@playwright/test';

test('renders verify prompt for a code without calling success state', async ({ page }) => {
	await page.goto('/verify/sample-code');

	await expect(page.getByRole('banner').getByRole('link', { name: 'HelpDesk' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Verify your email' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Verify email' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Account verified' })).toHaveCount(0);
});

test('shows missing-code UI without a verify button', async ({ page }) => {
	await page.goto('/verify/%20%20%20');

	await expect(page.getByRole('heading', { name: 'Missing verification code' })).toBeVisible();
	await expect(page.getByText('This verification link is incomplete.')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Verify email' })).toHaveCount(0);
	await expect(page.getByRole('alert').getByRole('link', { name: 'Create account' })).toBeVisible();
});

test('shows Identity unavailable error after Verify email click', async ({ page }) => {
	await page.goto('/verify/sample-code');

	await page.getByRole('button', { name: 'Verify email' }).click();

	await expect(
		page.getByText('Unable to reach the identity service. Please try again later.')
	).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Verify your email' })).toBeVisible();
});

test('login stub is reachable as the post-verify destination', async ({ page }) => {
	await page.goto('/login');

	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
	await expect(page.getByText('Login is not available yet.')).toBeVisible();
	await expect(page.getByRole('link', { name: 'Back to home' })).toBeVisible();
});
