import { defineConfig, devices } from "@playwright/test";

/**
 * MeetingFlow end-to-end tests.
 *
 * These tests drive the real React app in a real browser, which talks to the
 * real ASP.NET Core API and the real SQLite database. Start both before running:
 *
 *   MeetingFlow.ClientServer/MeetingFlow.Api   ->  dotnet run     (http://localhost:5062)
 *   MeetingFlow.ClientServer/MeetingFlow.Web   ->  npm run dev    (http://localhost:5173)
 */
export default defineConfig({
  testDir: "./tests",

  // The app has one shared SQLite database, so tests are not isolated from each
  // other yet. Run them one at a time until that is fixed.
  fullyParallel: false,
  workers: 1,
  retries: 0,

  reporter: [["list"], ["html", { open: "never" }]],

  use: {
    baseURL: "http://localhost:5173",

    // Keep the evidence you need to debug a failure.
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "off",
  },

  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
