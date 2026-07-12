import { expect, test } from '@playwright/test';

test('renders the Tailwind-styled landing page', async ({ page }) => {
	await page.goto('/');
	await expect(
		page.getByRole('heading', { name: 'A secure home for your support profile.' })
	).toBeVisible();
	await expect(
		page.getByText(
			'The SvelteKit frontend connects to the brokerless .NET service mesh through a server-only API boundary.'
		)
	).toBeVisible();
	await expect(
		page.getByRole('main').getByRole('link', { name: 'Create account' })
	).toHaveAttribute('href', '/register');
	await expect(page.getByRole('main').getByRole('link', { name: 'Sign in' })).toHaveAttribute(
		'href',
		'/login'
	);
});

test('shows anonymous shell chrome when there is no session', async ({ page }) => {
	await page.goto('/');

	const banner = page.getByRole('banner');
	await expect(banner.getByRole('link', { name: 'HelpDesk' })).toBeVisible();
	await expect(banner.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login');
	await expect(banner.getByRole('link', { name: 'Create account' })).toHaveAttribute(
		'href',
		'/register'
	);
	await expect(page.getByTestId('shell-profile')).toHaveCount(0);
});
