import { expect, test, type Page } from '@playwright/test';

async function fillLogin(page: Page, fields: { email: string; password: string }) {
	await page.getByLabel('Email').fill(fields.email);
	await page.getByLabel('Password', { exact: true }).fill(fields.password);
}

test('renders the login form under the shared shell', async ({ page }) => {
	await page.goto('/login');

	const banner = page.getByRole('banner');
	await expect(banner.getByRole('link', { name: 'HelpDesk' })).toBeVisible();
	await expect(banner.getByRole('link', { name: 'Sign in' })).toBeVisible();
	await expect(banner.getByRole('link', { name: 'Create account' })).toBeVisible();
	await expect(page.getByTestId('shell-profile')).toHaveCount(0);
	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
	await expect(page.getByLabel('Email')).toBeVisible();
	await expect(page.getByLabel('Password', { exact: true })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Forgot password?' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Create one' })).toBeVisible();
});

test('rejects empty fields after bypassing browser required', async ({ page }) => {
	await page.goto('/login');
	await page.locator('form').evaluate((form) => form.setAttribute('novalidate', ''));

	await page.getByRole('button', { name: 'Sign in' }).click();

	await expect(page.getByText('Email is required.')).toBeVisible();
	await expect(page.getByText('Password is required.')).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
});

test('rejects invalid email after bypassing browser validation', async ({ page }) => {
	await page.goto('/login');
	await page.locator('form').evaluate((form) => form.setAttribute('novalidate', ''));

	await fillLogin(page, {
		email: 'not-an-email',
		password: 'any-password'
	});
	await page.getByRole('button', { name: 'Sign in' }).click();

	await expect(page.getByText('Email must be a valid email address.')).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
	await expect(page.getByLabel('Email')).toHaveValue('not-an-email');
});

test('shows a form-level error when Identity is unavailable', async ({ page }) => {
	await page.goto('/login');

	await fillLogin(page, {
		email: 'user@example.test',
		password: 'long-enough-password'
	});
	await page.getByRole('button', { name: 'Sign in' }).click();

	await expect(
		page.getByText('Unable to reach the identity service. Please try again later.')
	).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
	await expect(page.getByLabel('Email')).toHaveValue('user@example.test');
});
