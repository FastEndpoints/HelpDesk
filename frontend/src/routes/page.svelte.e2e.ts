import { expect, test } from '@playwright/test';

test('renders the Tailwind-styled landing page', async ({ page }) => {
	await page.goto('/');
	await expect(
		page.getByRole('heading', { name: 'A secure home for your support profile.' })
	).toBeVisible();
	await expect(page.getByText('Frontend foundation ready')).toBeVisible();
});
