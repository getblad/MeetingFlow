// spec: specs/registration.md
// seed: tests/seed.spec.ts

import { test, expect } from '../../fixtures';

test.describe('Attendee Registration', () => {
  test('registers for a published meeting and persists the registration', async ({ page, attendee }) => {
    const meetingTitle = 'Product Engineering Meetup';
    const controlTitle = 'Cloud Integration Day';

    // Registrations count cell on whichever meeting detail page is currently open.
    const registrationsCell = page
      .getByRole('row')
      .filter({ has: page.getByRole('rowheader', { name: 'Registrations' }) })
      .getByRole('cell');

    const readRegistrationsCount = async () => Number((await registrationsCell.innerText()).trim());

    // 1. Obtain a unique attendee via the `attendee` fixture.
    // (the `attendee` fixture already produced a unique name/email for this test run)

    // 2. Navigate to `/` and open a Published meeting's detail page (e.g. "Product Engineering Meetup"). Read and store its current Registrations count.
    await page.goto('/');
    await page.getByRole('link', { name: meetingTitle }).click();
    const meetingBefore = await readRegistrationsCount();
    expect(Number.isInteger(meetingBefore)).toBe(true);

    // 3. Note the Registrations count of a second Published meeting (e.g. "Cloud Integration Day") as a control.
    await page.getByRole('link', { name: 'Meetings', exact: true }).click();
    await page.getByRole('link', { name: controlTitle }).click();
    const controlBefore = await readRegistrationsCount();

    // 4. Navigate to `/register`.
    await page.goto('/register');
    await expect(page.getByRole('heading', { name: 'Register for an Meeting', level: 1 })).toBeVisible();

    // 5. In the Meeting select, choose the first meeting from step 2.
    const meetingSelect = page.getByLabel('Meeting');
    // Match the option by its visible title text (not the full label, since the embedded date shifts
    // when seed data is regenerated), then select using the exact label text found.
    const meetingOptionLabel = (
      await meetingSelect.locator('option', { hasText: meetingTitle }).textContent()
    )?.trim();
    await meetingSelect.selectOption(meetingOptionLabel!);

    // 6. Fill Your Name with `attendee.name` and Your Email with `attendee.email`. Leave Ticket Type at its default.
    await page.getByLabel('Your Name').fill(attendee.name);
    await page.getByLabel('Your Email').fill(attendee.email);

    // 7. Click 'Register'.
    await page.getByRole('button', { name: 'Register' }).click();
    await expect(page.getByText('Registration created successfully!')).toBeVisible();

    // 8. Return to the first meeting's detail page and re-read the Registrations count.
    await page.goto('/');
    await page.getByRole('link', { name: meetingTitle }).click();
    await expect(registrationsCell).toHaveText(String(meetingBefore + 1));

    // 9. Open the control meeting's detail page from step 3.
    await page.goto('/');
    await page.getByRole('link', { name: controlTitle }).click();
    await expect(registrationsCell).toHaveText(String(controlBefore));
  });
});
