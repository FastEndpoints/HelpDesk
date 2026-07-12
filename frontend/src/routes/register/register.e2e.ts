import { expect, test, type Page } from '@playwright/test';

async function fillRegistration(
	page: Page,
	fields: { email: string; password: string; confirmPassword: string }
) {
	await page.getByLabel('Email').fill(fields.email);
	await page.getByLabel('Password', { exact: true }).fill(fields.password);
	await page.getByLabel('Confirm password').fill(fields.confirmPassword);
}

test('renders the registration form under the shared shell', async ({ page }) => {
	await page.goto('/register');

	await expect(page.getByRole('banner').getByRole('link', { name: 'HelpDesk' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Create account' })).toBeVisible();
	await expect(page.getByLabel('Email')).toBeVisible();
	await expect(page.getByLabel('Password', { exact: true })).toBeVisible();
	await expect(page.getByLabel('Confirm password')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create account' })).toBeVisible();
});

test('rejects mismatched passwords without leaving the form', async ({ page }) => {
	await page.goto('/register');

	await fillRegistration(page, {
		email: 'user@example.test',
		password: 'long-enough-password',
		confirmPassword: 'different-password'
	});
	await page.getByRole('button', { name: 'Create account' }).click();

	await expect(page.getByText('Passwords do not match.')).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Create account' })).toBeVisible();
	await expect(page.getByLabel('Email')).toHaveValue('user@example.test');
});

test('rejects short passwords after bypassing browser minlength', async ({ page }) => {
	await page.goto('/register');
	await page.locator('form').evaluate((form) => form.setAttribute('novalidate', ''));

	await fillRegistration(page, {
		email: 'user@example.test',
		password: 'too-short',
		confirmPassword: 'too-short'
	});
	await page.getByRole('button', { name: 'Create account' }).click();

	await expect(page.getByText('Password must be at least 12 characters.')).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Create account' })).toBeVisible();
});

test('shows a form-level error when Identity is unavailable', async ({ page }) => {
	await page.goto('/register');

	await fillRegistration(page, {
		email: 'user@example.test',
		password: 'long-enough-password',
		confirmPassword: 'long-enough-password'
	});
	await page.getByRole('button', { name: 'Create account' }).click();

	await expect(
		page.getByText('Unable to reach the identity service. Please try again later.')
	).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Create account' })).toBeVisible();
	await expect(page.getByLabel('Email')).toHaveValue('user@example.test');
});
