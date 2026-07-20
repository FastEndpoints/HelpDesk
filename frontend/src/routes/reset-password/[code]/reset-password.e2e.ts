import { expect, test } from '@playwright/test';

test('renders reset form for a code without success state', async ({ page }) => {
	await page.goto('/reset-password/sample-code');

	await expect(page.getByRole('banner').getByRole('link', { name: 'HelpDesk' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Choose a new password' })).toBeVisible();
	await expect(page.getByLabel('New password')).toBeVisible();
	await expect(page.getByLabel('Confirm password')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Update password' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'You can sign in' })).toHaveCount(0);
});

test('shows missing-code UI without a submit button', async ({ page }) => {
	await page.goto('/reset-password/%20%20%20');

	await expect(page.getByRole('heading', { name: 'Missing reset code' })).toBeVisible();
	await expect(page.getByText('This password reset link is incomplete.')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Update password' })).toHaveCount(0);
	await expect(
		page.getByRole('alert').getByRole('link', { name: 'Request a new link' })
	).toBeVisible();
});

test('rejects short password and mismatch after bypassing browser validation', async ({ page }) => {
	await page.goto('/reset-password/sample-code');
	await page.locator('form').evaluate((form) => form.setAttribute('novalidate', ''));

	await page.getByLabel('New password').fill('short');
	await page.getByLabel('Confirm password').fill('different');
	await page.getByRole('button', { name: 'Update password' }).click();

	await expect(page.getByText('Password must be at least 12 characters.')).toBeVisible();
	await expect(page.getByText('Passwords do not match.')).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Choose a new password' })).toBeVisible();
});

test('shows Identity unavailable error after Update password click', async ({ page }) => {
	await page.goto('/reset-password/sample-code');

	await page.getByLabel('New password').fill('long-enough-password');
	await page.getByLabel('Confirm password').fill('long-enough-password');
	await page.getByRole('button', { name: 'Update password' }).click();

	await expect(
		page.getByText('Unable to reach the identity service. Please try again later.')
	).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Choose a new password' })).toBeVisible();
});
