import { test as base, expect } from '@playwright/test';

type Fixtures = {
  attendee: { name: string; email: string };
};

export const test = base.extend<Fixtures>({
  // A unique attendee per test, so reruns never collide.
  attendee: async ({}, use, testInfo) => {
    const stamp = `${Date.now()}-${testInfo.workerIndex}`;
    await use({
      name: `Test Attendee ${stamp}`,
      email: `e2e+${stamp}@meetingflow.test`,
    });
  },
});

export { expect };