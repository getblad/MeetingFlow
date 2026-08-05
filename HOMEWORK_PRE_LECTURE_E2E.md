# Pre-Lecture Homework: End-to-End Testing with Playwright

> **Goal:** Set up Playwright, learn the shape of an E2E test, and then write three
> tests against the running MeetingFlow app. Each one will fail. Each failure is a
> real bug in this repository. Your job is to read the failure, find the cause, and
> fix the implementation until the test passes.
>
> Unlike the previous homework, this one has right answers. Bring your fixes.

---

## What you are testing

`MeetingFlow.ClientServer` — the React SPA on `http://localhost:5173` talking to the
ASP.NET Core API on `http://localhost:5062`.

An end-to-end test does not import your components or call your methods. It opens a
real browser, clicks real buttons, and asserts on what a user can actually see. That
is its whole value: it is the only layer that notices when two correct-looking screens
disagree with each other.

---

## Part 0 — Setup (~15 minutes)

### Start the application

You need **two terminals** running before any test will pass.

```bash
# Terminal 1 — backend
cd MeetingFlow.ClientServer/MeetingFlow.Api
dotnet run                     # http://localhost:5062
```

```bash
# Terminal 2 — frontend
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install
npm run dev                    # http://localhost:5173
```

Open http://localhost:5173 and confirm you see a list of meeting cards.

> The SQLite database is created and seeded on first run. If you ever want a clean
> slate: stop the API, delete `MeetingFlow.Api/meetingflow_api.db`, and start it again.

### Install Playwright

The test project already exists at `MeetingFlow.ClientServer/e2e`.

```bash
# Terminal 3 — this is where you will work
cd MeetingFlow.ClientServer/e2e
npm install
npx playwright install chromium
```

### Verify

```bash
npm test
```

You should see one passing test:

```
Running 1 test using 1 worker
  ✓  1 [chromium] › smoke.spec.ts:8:1 › the meetings page loads (742ms)

  1 passed (1.2s)
```

If it fails, nothing below will work. Check that both servers are up and that
http://localhost:5173 loads in your own browser first.

---

## Part 1 — The anatomy of a Playwright test (~10 minutes)

Open `e2e/tests/smoke.spec.ts`:

```ts
import { test, expect } from "@playwright/test";

test("the meetings page loads", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "Meetings", level: 1 })).toBeVisible();
  await expect(page.getByRole("link", { name: "Frontend Architecture Summit" })).toBeVisible();
});
```

Five things are happening, and every test you write will have the same shape:

| Piece                       | What it is                                                                                    |
| --------------------------- | --------------------------------------------------------------------------------------------- |
| `test("name", async ...)`   | One scenario. The name is what appears in the report — make it a sentence about behaviour.    |
| `{ page }`                  | A **fixture**. Playwright gives each test a fresh browser context, so tests do not share state. |
| `page.goto("/")`            | Navigation. `/` is relative to `baseURL` in `playwright.config.ts`.                            |
| `page.getByRole(...)`       | A **locator** — a description of an element, not the element itself. Nothing has run yet.      |
| `await expect(...).toBe...` | A **web-first assertion**. It retries until the condition holds or the timeout expires.        |

### The one idea that matters: auto-waiting

You will never need `sleep`, `waitForTimeout`, or a manual retry loop. A locator is
resolved at the moment it is used, and web-first assertions retry automatically.

```ts
// ✗ Don't
await page.waitForTimeout(2000);
expect(await page.locator("h1").textContent()).toBe("Meetings");

// ✓ Do
await expect(page.getByRole("heading", { level: 1 })).toHaveText("Meetings");
```

The second version waits for exactly as long as it needs to and no longer. If you find
yourself adding a sleep, it means you have not identified the state you are waiting for.

### Structure a test as Arrange → Act → Assert

```ts
test("a visitor can open a meeting", async ({ page }) => {
  // Arrange
  await page.goto("/");

  // Act
  await page.getByRole("link", { name: "Cloud Integration Day" }).click();

  // Assert
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("Cloud Integration Day");
});
```

Try that one yourself — add it to `smoke.spec.ts` and run `npm test`. It should pass.

---

## Part 2 — Locators and assertions, the short version (~10 minutes)

### Choosing a locator

Prefer locators that describe what the **user** sees. In rough order of preference:

| Locator                                     | Use for                                 | Example                                                |
| ------------------------------------------- | --------------------------------------- | ------------------------------------------------------ |
| `getByRole(role, { name })`                 | Buttons, links, headings, checkboxes    | `getByRole("button", { name: "Register" })`            |
| `getByLabel(text)`                          | Form fields                             | `getByLabel("Your Email")`                             |
| `getByText(text)`                           | Static, user-visible copy               | `getByText("Registration created successfully!")`      |
| `getByTestId(id)`                           | When there is genuinely no semantics    | `getByTestId("total-meetings")`                        |
| `locator(cssSelector)`                      | **Last resort**                         | `locator(".stat-card")`                                |

Why the order? A role-based locator breaks when the *behaviour* changes. A CSS locator
breaks when someone renames a class. Only one of those is a real signal.

> **You will need a CSS locator once in this homework.** When you do, write down why —
> it is a finding about the application, not about Playwright.

### Assertions you will use

```ts
await expect(locator).toBeVisible();
await expect(locator).toHaveText("exact text");
await expect(locator).toContainText("substring");
await expect(locator).toHaveCount(3);
await expect(locator).toHaveValue("");
await expect(page).toHaveURL(/\/meetings\//);
```

`toHaveCount(0)` is how you assert something is **absent**. You will use it in Part 3.

### Three ways to run your tests

```bash
npm test              # headless, fastest, what CI would run
npm run test:headed   # watch the browser
npm run test:ui       # interactive UI mode — pick a test, step through it, inspect locators
```

**Use UI mode while writing tests.** It shows you the DOM at every step and lets you
try locators against the live page. It will save you most of the time this homework costs.

---

## Part 3 — Test 1: the public meeting catalogue (~15 minutes)

### The requirement

> A visitor to the public meeting list should only see meetings they can actually
> attend. Meetings that are still `Draft` or have been `Cancelled` are not public.

Check the seed data in `MeetingFlow.Api/Data/SeedData.cs` — there are five meetings:
three `Published`, one `Draft` ("Distributed Systems Workshop"), one `Cancelled`
("AI Tools for Developers").

### Your task

Create `e2e/tests/meetings.spec.ts`:

```ts
import { test, expect } from "@playwright/test";

test("the public catalogue lists only published meetings", async ({ page }) => {
  await page.goto("/");

  // Every meeting card renders its title as a level-3 heading.
  await expect(page.getByRole("heading", { level: 3 })).toHaveCount(3);

  // The three published meetings are there...
  await expect(page.getByRole("link", { name: "Frontend Architecture Summit" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Cloud Integration Day" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Product Engineering Meetup" })).toBeVisible();

  // ...and the draft and cancelled ones are not.
  await expect(page.getByRole("link", { name: "Distributed Systems Workshop" })).toHaveCount(0);
  await expect(page.getByRole("link", { name: "AI Tools for Developers" })).toHaveCount(0);
});
```

Run it:

```bash
npm test
```

### Read the failure

```
  ✘  1 [chromium] › meetings.spec.ts:3:1 › the public catalogue lists only published meetings

    Error: Timed out 5000ms waiting for expect(locator).toHaveCount(expected)

    Locator: getByRole('heading', { level: 3 })
    Expected: 3
    Received: 5
```

Playwright tells you what it looked for, what it expected, and what it got. Before you
touch any code, look at the evidence:

```bash
npm run report
```

The HTML report opens. Click the failed test, then open the **trace**. You get a
timeline, a DOM snapshot at every step, and — most usefully here — the **Network** tab.

### Find the cause

1. In the trace's Network tab, find the request the page made on load.
2. Look at the response. How many meetings came back? What are their `status` values?
3. Now open the endpoint that served it: `MeetingFlow.Api/Endpoints/MeetingsEndpoints.cs`.
4. Compare it with `/api/dashboard` in `DashboardEndpoints.cs` — how does *that* one
   decide which meetings are public?

Write down, in one sentence, where the bug is before you read on.

<details>
<summary><strong>Bug 1 — the fix</strong></summary>

`GET /api/meetings` returns every meeting regardless of status. The React page renders
whatever it is given, so `Draft` and `Cancelled` meetings appear in the public list.

Note that the registration form (`CreateRegistrationPage.tsx`) filters to `Published`
on the client, and the dashboard filters to `Published` on the server. The catalogue
is the odd one out.

In `MeetingFlow.Api/Endpoints/MeetingsEndpoints.cs`:

```csharp
app.MapGet("/api/meetings", async (MeetingFlowDbContext db) =>
{
    var meetings = await db.Meetings
        .Where(e => e.Status == "Published")     // <-- add this
        .Include(e => e.Venue)
        .Include(e => e.Sessions)
        .ToListAsync();

    return Results.Ok(meetings.OrderBy(e => e.StartsAt).ToList());
});
```

Restart the API (`dotnet run`) and run the test again. It should pass — and so should
the smoke test.

</details>

### Question to answer

Could a unit test have caught this? Could a component test? Say precisely which layer
would have to change for each to be possible.

---

## Part 4 — Test 2: registering for a meeting (~20 minutes)

### The requirement

> A visitor can pick a published meeting, enter their name and email, choose a ticket
> type, submit, and see a confirmation.

### Your task

Create `e2e/tests/registration.spec.ts`:

```ts
import { test, expect } from "@playwright/test";

test("a visitor can register for a published meeting", async ({ page }) => {
  await page.goto("/register");

  await expect(page.getByRole("heading", { name: "Register for a Meeting" })).toBeVisible();

  // A unique email per run, so repeated runs do not collide.
  const email = `e2e-${Date.now()}@meetingflow.test`;

  await page.getByLabel("Meeting", { exact: true }).selectOption({ index: 1 });
  await page.getByLabel("Your Name").fill("Test Attendee");
  await page.getByLabel("Your Email").fill(email);
  await page.getByLabel("Ticket Type").selectOption("VIP");

  await page.getByRole("button", { name: "Register" }).click();

  await expect(page.getByText("Registration created successfully!")).toBeVisible();
});
```

### Read the first failure

```
    Error: expect(locator).toBeVisible()

    Locator: getByRole('heading', { name: 'Register for a Meeting' })
    Expected: visible
    Received: <element(s) not found>
```

Open `/register` in your browser and read the page heading carefully. Then open
`MeetingFlow.Web/src/pages/CreateRegistrationPage.tsx` and find it.

<details>
<summary><strong>Bug 2a — the fix</strong></summary>

The heading reads **"Register for an Meeting"**. Fix the article:

```tsx
<h1>Register for a Meeting</h1>
```

</details>

### Read the second failure

Run again. Now you get something different:

```
    Error: locator.selectOption: Test timeout of 30000ms exceeded.

    Call log:
      - waiting for getByLabel('Meeting', { exact: true })
```

Playwright cannot find a form field labelled "Meeting" — even though you can plainly
see the word "Meeting" above the dropdown.

**This is the interesting one.** Open the DOM snapshot in the trace, or inspect the
form in your browser's dev tools, and look at how the `<label>` and the `<select>`
relate to each other.

<details>
<summary><strong>Bug 2b — the fix</strong></summary>

The labels are not associated with their controls:

```tsx
<div className="field">
  <label>Meeting</label>
  <select value={meetingId} ...>
```

A `<label>` only labels a control if it either wraps the control or carries a `htmlFor`
that matches the control's `id`. Here it does neither, so as far as the accessibility
tree is concerned these inputs have **no name at all**.

That is not only a testing problem. A screen-reader user hears "combo box" with no idea
what it selects, and clicking the label does not focus the field.

In `MeetingFlow.Web/src/pages/CreateRegistrationPage.tsx`, wire up all four fields:

```tsx
<div className="field">
  <label htmlFor="meeting">Meeting</label>
  <select id="meeting" value={meetingId} onChange={(e) => setMeetingId(e.target.value)} required>
```

```tsx
<div className="field">
  <label htmlFor="attendee-name">Your Name</label>
  <input id="attendee-name" type="text" value={attendeeName} onChange={...} required />
</div>

<div className="field">
  <label htmlFor="attendee-email">Your Email</label>
  <input id="attendee-email" type="email" value={attendeeEmail} onChange={...} required />
</div>

<div className="field">
  <label htmlFor="ticket-type">Ticket Type</label>
  <select id="ticket-type" value={ticketType} onChange={...}>
```

Vite hot-reloads, so no restart is needed. Run the test again — it should pass.

</details>

### Questions to answer

1. The test failed because the *application* was not accessible, not because the test
   was wrong. How often do you think that is true of E2E failures in general?
2. `selectOption({ index: 1 })` picks the first real meeting. Why is index `1` and not
   `0`? What would break this test if someone reordered the dropdown?
3. The test generates a unique email every run. What would go wrong if it used a fixed
   one? Try it and see.

---

## Part 5 — Test 3: two screens that disagree (~15 minutes)

You have now fixed the catalogue. Look at the app again — the meetings page shows
three meetings, and the dashboard says something else.

### The requirement

> The "Total Meetings" figure on the dashboard describes the same set of meetings a
> visitor can see in the catalogue. The two screens must not contradict each other.

### Your task

Create `e2e/tests/dashboard.spec.ts`:

```ts
import { test, expect } from "@playwright/test";

test("the dashboard total matches the public catalogue", async ({ page }) => {
  // Count what a visitor can actually see.
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 3 }).first()).toBeVisible();
  const visibleMeetings = await page.getByRole("heading", { level: 3 }).count();

  // Compare it with what the dashboard claims.
  await page.goto("/dashboard");

  // No semantic markup here — see the note below.
  const totalMeetings = page.locator(".stat-card", { hasText: "Total Meetings" }).locator(".number");

  await expect(totalMeetings).toHaveText(String(visibleMeetings));
});
```

> **Note the CSS locator.** The stat cards are `<div>`s with no heading, label or role,
> so there is nothing semantic to grab. That is the "last resort" case from Part 2.
> Write it down — it is a testability finding worth raising.

### Read the failure

```
    Error: Timed out 5000ms waiting for expect(locator).toHaveText(expected)

    Expected string: "3"
    Received string: "5"
```

### Find the cause

Open `MeetingFlow.Api/Endpoints/DashboardEndpoints.cs` and read the first line of the
handler. Then read the line that builds `upcomingMeetings`, a few lines below. They do
not agree with each other.

<details>
<summary><strong>Bug 3 — the fix</strong></summary>

`totalMeetings` counts every row in the table, while `upcomingMeetings` — in the same
handler — filters to `Published`. The dashboard contradicts itself as well as the
catalogue.

```csharp
var totalMeetings = await db.Meetings.CountAsync(e => e.Status == "Published");
```

</details>

### A decision, not just a fix

There were two defensible ways to make this test pass:

- Count only published meetings (what the fix above does), or
- Rename the card to "All Meetings" and change the test to assert something else.

You picked one. **Write down which, and why.** A failing test asks a question; it does
not answer it. Deciding what the correct behaviour *is* remains a human job — that is
the thread we pick up in the lecture.

---

## Part 6 — Bonus (~10 minutes, optional)

Only if you have time.

### 6a — The fix in Part 3 was incomplete

Hiding a meeting from the list is not the same as making it non-public. Try:

```ts
test("a cancelled meeting is not reachable by direct link", async ({ page }) => {
  // "AI Tools for Developers" — Cancelled
  await page.goto("/meetings/b2000000-0000-0000-0000-000000000005");
  await expect(page.getByRole("heading", { name: "AI Tools for Developers" })).toHaveCount(0);
});
```

Does it pass? What would you have to change to make it pass, and is that the right
change? (There is a reasonable argument for leaving it alone — make it.)

### 6b — A cosmetic bug you can see with your eyes

Look at the meeting card for "Cloud Integration Day" on the home page. Its description
ends in `...` even though nothing was truncated. Find it in
`MeetingFlow.Web/src/components/MeetingCard.tsx` and fix it. Then write a test that
would have caught it — and decide whether that test is worth keeping.

---

## What to bring to the lecture

1. **Your three test files** — `meetings.spec.ts`, `registration.spec.ts`, `dashboard.spec.ts`
2. **Your fixes** — all four of them, on a branch
3. **Your written answers** to the questions in Parts 3, 4 and 5
4. **One sentence** answering this: *how did you know these were the three things worth
   testing?* You were told. That is the question the lecture is about.

---

## Summary of deliverables

| #   | Task                                                    | Time   | Required? |
| --- | ------------------------------------------------------- | ------ | --------- |
| 0   | Setup — run the app, install Playwright, green smoke test | 15 min | Yes       |
| 1   | Read the anatomy of a test, add one of your own          | 10 min | Yes       |
| 2   | Locators and assertions; try UI mode                     | 10 min | Yes       |
| 3   | Test 1 — catalogue → **Bug 1** (public list)             | 15 min | Yes       |
| 4   | Test 2 — registration → **Bug 2a + 2b** (heading, labels)| 20 min | Yes       |
| 5   | Test 3 — dashboard → **Bug 3** (total count)             | 15 min | Yes       |
| 6   | Bonus — incomplete fix, cosmetic bug                     | 10 min | Bonus     |

**Total: ~85 minutes** (75 without the bonus)

---

## Troubleshooting

| Symptom                                                | Cause                                                                 |
| ------------------------------------------------------ | --------------------------------------------------------------------- |
| Every test times out on `page.goto`                    | The Vite dev server is not running on 5173                            |
| The page loads but shows "Error: HTTP 500" or is empty | The API is not running on 5062                                        |
| Backend changes have no effect                         | `dotnet run` does not hot-reload — stop and restart it                 |
| Counts are off by one after several runs               | Your registrations are accumulating; delete `meetingflow_api.db` and restart the API |
| `npx playwright install` fails                         | Corporate proxy — set `HTTPS_PROXY`, or ask before the session         |
