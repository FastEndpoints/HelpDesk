import { expect, test } from '@playwright/test';

test('renders the forgot-password form under the shared shell', async ({ page }) => {
	await page.goto('/forgot-password');

	const banner = page.getByRole('banner');
	await expect(banner.getByRole('link', { name: 'HelpDesk' })).toBeVisible();
	await expect(banner.getByRole('link', { name: 'Sign in' })).toBeVisible();
	await expect(page.getByTestId('shell-profile')).toHaveCount(0);
	await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeVisible();
	await expect(page.getByLabel('Email')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Send reset link' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Sign in' }).last()).toBeVisible();
});

test('rejects empty email after bypassing browser required', async ({ page }) => {
	await page.goto('/forgot-password');
	await page.locator('form').evaluate((form) => form.setAttribute('novalidate', ''));

	await page.getByRole('button', { name: 'Send reset link' }).click();

	await expect(page.getByText('Email is required.')).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeVisible();
});

test('rejects invalid email after bypassing browser validation', async ({ page }) => {
	await page.goto('/forgot-password');
	await page.locator('form').evaluate((form) => form.setAttribute('novalidate', ''));

	await page.getByLabel('Email').fill('not-an-email');
	await page.getByRole('button', { name: 'Send reset link' }).click();

	await expect(page.getByText('Email must be a valid email address.')).toBeVisible();
	await expect(page.getByLabel('Email')).toHaveValue('not-an-email');
});

test('shows a form-level error when Identity is unavailable', async ({ page }) => {
	await page.goto('/forgot-password');

	await page.getByLabel('Email').fill('user@example.test');
	await page.getByRole('button', { name: 'Send reset link' }).click();

	await expect(
		page.getByText('Unable to reach the identity service. Please try again later.')
	).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeVisible();
	await expect(page.getByLabel('Email')).toHaveValue('user@example.test');
});

test('login Forgot password link opens this page', async ({ page }) => {
	await page.goto('/login');

	await page.getByRole('link', { name: 'Forgot password?' }).click();

	await expect(page).toHaveURL(/\/forgot-password$/);
	await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeVisible();
});
