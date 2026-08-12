# Pre-Lecture Homework: End-to-End Testing with Playwright

> **Goal:** Set up Playwright and write two end-to-end tests against the running
> MeetingFlow app. Both will fail. Each failure is a real bug in this repository —
> read the failure, find the cause, and fix the implementation until the test passes.
>
> Unlike the previous homework, this one has right answers. Bring your fixes.

---

## What you are testing

`MeetingFlow.ClientServer` — the React SPA on `http://localhost:5173` talking to the
ASP.NET Core API on `http://localhost:5062`.

An E2E test does not import your components or call your methods. It opens a real
browser, clicks real buttons, and asserts on what a user can see. That is its whole
value: it is the only layer that notices when two correct-looking screens disagree.

---

## Part 0 — Setup (~15 minutes)

You need **three terminals**.

```bash
# 1 — backend
cd MeetingFlow.ClientServer/MeetingFlow.Api
dotnet run                     # http://localhost:5062

# 2 — frontend
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install && npm run dev     # http://localhost:5173

# 3 — tests (your working terminal)
cd MeetingFlow.ClientServer/e2e
npm install
npx playwright install chromium
npm test
```

Open http://localhost:5173 first and confirm you see meeting cards. Then `npm test`
should report one passing test:

```
  ✓  1 [chromium] › smoke.spec.ts:8:1 › the meetings page loads (742ms)
```

If it fails, nothing below will work — check both servers before continuing.

> The SQLite database is created and seeded on first run. To reset it: stop the API,
> delete `MeetingFlow.Api/meetingflow_api.db`, start it again.

---

## Part 1 — Read the example (~10 minutes)

Open `e2e/tests/smoke.spec.ts`. Every test you write has this shape:

| Piece                       | What it is                                                                                    |
| --------------------------- | --------------------------------------------------------------------------------------------- |
| `test("name", async ...)`   | One scenario. Name it after the behaviour, not the mechanics.                                  |
| `{ page }`                  | A **fixture** — each test gets a fresh browser context, so tests do not share state.            |
| `page.goto("/")`            | Relative to `baseURL` in `playwright.config.ts`.                                                |
| `page.getByRole(...)`       | A **locator** — a description of an element. Nothing has run yet.                               |
| `await expect(...).toBe...` | A **web-first assertion** — retries until the condition holds or the timeout expires.           |

### The one idea that matters: auto-waiting

You never need `waitForTimeout` or a retry loop. Locators resolve when used, and
web-first assertions retry on their own.

```ts
// ✗ Don't
await page.waitForTimeout(2000);
expect(await page.locator("h1").textContent()).toBe("Meetings");

// ✓ Do
await expect(page.getByRole("heading", { level: 1 })).toHaveText("Meetings");
```

If you reach for a sleep, it means you have not identified the state you are waiting for.

### Choosing a locator

Prefer what the **user** sees. A role-based locator breaks when the behaviour changes;
a CSS locator breaks when someone renames a class. Only one of those is a real signal.

| Locator                     | Use for                              |
| --------------------------- | ------------------------------------ |
| `getByRole(role, { name })` | Buttons, links, headings             |
| `getByLabel(text)`          | Form fields                          |
| `getByText(text)`           | Static, user-visible copy            |
| `locator(css)`              | **Last resort** — write down why     |

Assertions you will need: `toBeVisible()`, `toHaveText()`, `toHaveCount()`.
`toHaveCount(0)` is how you assert something is **absent**.

### Running tests

`npm test` (headless) · `npm run test:ui` (interactive) · `npm run report` (last run).

**Use UI mode while writing.** It shows the DOM at every step and lets you try locators
against the live page. It will save you most of the time this homework costs.

---

## Part 2 — Test 1: the public catalogue (~15 minutes)

### The requirement

> A visitor to the public meeting list should only see meetings they can actually
> attend. Meetings that are still `Draft` or have been `Cancelled` are not public.

Read `MeetingFlow.Api/Data/SeedData.cs` and note which of the five seeded meetings
are `Published`, which is `Draft`, and which is `Cancelled`.

### Your task

Write `e2e/tests/meetings.spec.ts` that verifies, on the home page (`/`):

1. Exactly three meeting cards are rendered.
2. Each of the three published meetings is visible by title.
3. Neither the draft nor the cancelled meeting appears.

**Hints:**

- Every meeting card renders its title as a level-3 heading — `getByRole("heading", { level: 3 })`
  gives you all the cards on the page.
- Card titles are links to the details page.
- Use `toHaveCount(0)` for the two that should not be there.

### When it fails

Read the reporter output first, then `npm run report` and open the **trace**. In the
trace's **Network** tab, find the request the page made on load: how many meetings came
back, and what are their `status` values?

Then compare two files: `MeetingFlow.Api/Endpoints/MeetingsEndpoints.cs` and
`MeetingFlow.Api/Endpoints/DashboardEndpoints.cs`. How does each one decide which
meetings are public? Write down where the bug is before you look at the answer.

<details>
<summary><strong>Bug 1 — the fix</strong></summary>

`GET /api/meetings` returns every meeting regardless of status, and the React page
renders whatever it is given. The registration form filters to `Published` on the
client and the dashboard filters on the server — the catalogue is the odd one out.

In `MeetingsEndpoints.cs`:

```csharp
var meetings = await db.Meetings
    .Where(e => e.Status == "Published")     // <-- add this
    .Include(e => e.Venue)
    .Include(e => e.Sessions)
    .ToListAsync();
```

Restart the API (`dotnet run` does not hot-reload) and run the test again.

</details>

**Question:** could a unit test have caught this? A component test? Say precisely what
would have to change for each to be possible.

---

## Part 3 — Test 2: registering for a meeting (~20 minutes)

### The requirement

> A visitor can pick a published meeting, enter their name and email, choose a ticket
> type, submit, and see a confirmation.

Do this one in the UI yourself first, so you know what the flow looks like.

### Your task

Write `e2e/tests/registration.spec.ts` that verifies, on `/register`:

1. The page heading reads "Register for a Meeting".
2. A visitor can select a meeting, fill in name and email, pick a ticket type, and submit.
3. The confirmation message appears afterwards.

**Hints:**

- Use `getByLabel` for the four form fields — that is the locator this markup should support.
- `selectOption({ index: 1 })` picks the first real option; index `0` is the placeholder.
- Generate a unique email per run (`` `e2e-${Date.now()}@meetingflow.test` ``) so repeated
  runs do not collide.
- The confirmation is plain text — `getByText` is right here.

> **You will hit two separate failures in this part.** Both are bugs in the application,
> not in your test. Fix the first, rerun, and deal with the second.

### When it fails the first time

Compare the assertion message with what you see on the page. It is a one-character bug.

<details>
<summary><strong>Bug 2a — the fix</strong></summary>

`CreateRegistrationPage.tsx` line 57 reads `<h1>Register for an Meeting</h1>`.
Fix the article: `Register for a Meeting`.

</details>

### When it fails the second time

Playwright cannot find a form field labelled "Meeting" — even though you can plainly
see the word above the dropdown. **This is the interesting one.**

Inspect the form in dev tools, or open the DOM snapshot in the trace, and look at how
the `<label>` and the `<select>` relate to each other.

<details>
<summary><strong>Bug 2b — the fix</strong></summary>

None of the four labels are associated with their controls:

```tsx
<label>Meeting</label>
<select value={meetingId} ...>
```

A `<label>` only labels a control if it wraps it or carries a `htmlFor` matching the
control's `id`. Here it does neither — so in the accessibility tree these inputs have
**no name at all**. A screen-reader user hears "combo box" with no idea what it selects,
and clicking the label does not focus the field.

Wire up all four fields in `CreateRegistrationPage.tsx`:

```tsx
<label htmlFor="meeting">Meeting</label>
<select id="meeting" value={meetingId} ... >
```

…and the same for Your Name, Your Email and Ticket Type. Vite hot-reloads, so no restart.

</details>

**Questions:**

1. The test failed because the *application* was not accessible, not because the test
   was wrong. How often do you think that is true of E2E failures in general?
2. What would break this test if someone reordered the meeting dropdown?
3. What would go wrong if the test used a fixed email instead of a generated one? Try it.

---

## Part 4 — Bonus (~10 minutes, optional)

**4a.** Now that the catalogue is fixed, open `/dashboard`. Does "Total Meetings" agree
with what a visitor can see on `/`? Write a test that compares the two screens, then
decide what the correct fix is — there is more than one defensible answer, and choosing
is a human job.

**4b.** Hiding a meeting from the list is not the same as making it non-public. Try
navigating straight to `/meetings/b2000000-0000-0000-0000-000000000005` (the cancelled
meeting). Should that work? Argue either way.

---

## What to bring to the lecture

1. **Your two test files** — `meetings.spec.ts`, `registration.spec.ts`
2. **Your three fixes**, on a branch
3. **Written answers** to the questions in Parts 2 and 3
4. **One sentence** answering this: *how did you know these were the things worth
   testing?* You were told. That is the question the lecture is about.

---

## Summary of deliverables

| #   | Task                                                          | Time   | Required? |
| --- | ------------------------------------------------------------- | ------ | --------- |
| 0   | Setup — run the app, install Playwright, green smoke test      | 15 min | Yes       |
| 1   | Read the example test; try UI mode                             | 10 min | Yes       |
| 2   | Test 1 — catalogue → **Bug 1** (public list)                   | 15 min | Yes       |
| 3   | Test 2 — registration → **Bug 2a + 2b** (heading, labels)      | 20 min | Yes       |
| 4   | Bonus — dashboard consistency, direct links                    | 10 min | Bonus     |

**Total: ~60 minutes** (70 with the bonus)

---

## Troubleshooting

| Symptom                                     | Cause                                                        |
| ------------------------------------------- | ------------------------------------------------------------ |
| Every test times out on `page.goto`         | Vite is not running on 5173                                   |
| Page loads but shows an error or is empty   | The API is not running on 5062                                |
| Backend changes have no effect              | `dotnet run` does not hot-reload — stop and restart it        |
| Counts drift after several runs             | Delete `meetingflow_api.db` and restart the API               |
| `npx playwright install` fails              | Corporate proxy — set `HTTPS_PROXY`, or ask before the session |
