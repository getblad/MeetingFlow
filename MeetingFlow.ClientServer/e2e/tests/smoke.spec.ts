import { test, expect } from "@playwright/test";

/**
 * A worked example. This test should pass against the current code.
 * Use it as the template for the tests in the homework.
 */
test("the meetings page loads", async ({ page }) => {
  // Arrange — go to the page under test.
  await page.goto("/");

  // Assert — a web-first assertion waits for the condition instead of sleeping.
  await expect(page.getByRole("heading", { name: "Meetings", level: 1 })).toBeVisible();
  await expect(page.getByRole("link", { name: "Frontend Architecture Summit" })).toBeVisible();
});
