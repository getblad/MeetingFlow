import { test, expect } from '../fixtures';

test('seed', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Meetings', level: 1 })).toBeVisible();
});