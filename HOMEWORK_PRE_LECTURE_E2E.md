# Pre-Lecture Homework: End-to-End Testing with Playwright

> **Goal:** Set up Playwright, read a little of the code, then write two end-to-end
> tests against the running MeetingFlow app. Both will fail. Each failure is a real
> bug in this repository — read the failure, find the cause, and fix the
> implementation until the test passes.
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

## Part 0 — Setup (~10 minutes)

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

### Where the e2e project came from

You did not have to create it. Below is how a Playwright project is initialised, and
the settings we did not leave at their defaults.

```bash
npm init playwright@latest
```

It writes `package.json`, `playwright.config.ts`, a `tests/` folder with an example
spec, and a `.gitignore`. Ours is a trimmed version of that output.

| Setting                              | Why                                                                                        |
| ------------------------------------ | -------------------------------------------------------------------------------------------- |
| `baseURL: "http://localhost:5173"`   | Tests can say `page.goto("/")`.                                                             |
| `trace: "retain-on-failure"`         | Keeps the timeline, DOM snapshots and network log of a failed test — Parts 3 and 4 need it. |
| `workers: 1`, `fullyParallel: false` | One shared SQLite database, so tests are not isolated. A workaround, not a good end state.  |
| `projects: [chromium]`               | One browser; cross-browser runs are a CI concern.                                           |

`tests/smoke.spec.ts` replaces the generated example. `tsconfig.json` is there for
editor type-checking — Playwright does not need it to run tests.

---

## Part 1 — Read the code (~10 minutes)

Open these files and read them. No changes yet.

### What to look at

| File                                                  | What to look at                                                                                          |
| ----------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `MeetingFlow.Api/Endpoints/MeetingsEndpoints.cs`      | `GET /api/meetings` — what decides which meetings a visitor gets back?                                    |
| `MeetingFlow.Api/Endpoints/DashboardEndpoints.cs`     | The same question, asked of a different endpoint. Do the two answers agree with each other?               |
| `MeetingFlow.Web/src/pages/CreateRegistrationPage.tsx` | Read the `<h1>` out loud. Then look at how each `<label>` is connected to its `<select>` or `<input>`.     |
| `MeetingFlow.Api/Data/SeedData.cs`                    | Of the five seeded meetings, which are `Published`? Which one is `Draft`, and which is `Cancelled`?       |
| `e2e/playwright.config.ts`                            | What is `baseURL`? What does `trace: "retain-on-failure"` give you when something breaks?                  |

### Reflection questions (write down your answers)

1. Two endpoints in the same API answer the question "which meetings are public?".
   Do they answer it the same way? If not — which screen would a user believe?

2. Open `/register` in your browser and click on the word **Meeting** above the
   dropdown. Does the dropdown get focus? What does that tell you about the markup?

3. Both of the things you just noticed are visible in a browser inside a minute.
   Why do you think neither has been caught?

---

## Part 2 — Playwright in five minutes (~5 minutes)

Open `e2e/tests/smoke.spec.ts`. Every test you write has this shape:

| Piece                       | What it is                                                                            |
| --------------------------- | --------------------------------------------------------------------------------------- |
| `test("name", async ...)`   | One scenario. Name it after the behaviour, not the mechanics.                             |
| `{ page }`                  | A **fixture** — each test gets a fresh browser context, so tests do not share state.       |
| `page.goto("/")`            | Relative to `baseURL` in `playwright.config.ts`.                                          |
| `page.getByRole(...)`       | A **locator** — a description of an element. Nothing has run yet.                          |
| `await expect(...).toBe...` | A **web-first assertion** — retries until the condition holds or the timeout expires.      |

**Locators, in order of preference:** `getByRole(role, { name })` for buttons, links and
headings · `getByLabel` for form fields · `getByText` for static copy · `locator(css)` as a
**last resort**. A role-based locator breaks when the behaviour changes; a CSS locator
breaks when someone renames a class. Only one of those is a real signal.

**Assertions you will need:** `toBeVisible()`, `toHaveText()`, `toHaveCount()`.
`toHaveCount(0)` is how you assert something is **absent**.

**Running:** `npm test` (headless) · `npm run test:ui` (interactive) · `npm run report`.

---

## Part 3 — Test 1: the public catalogue (~15 minutes)

### The requirement

> A visitor to the public meeting list should only see meetings they can actually
> attend. Meetings that are still `Draft` or have been `Cancelled` are not public.

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

Then go back to the two endpoint files from Part 1. Write down where the bug is before
you open the answer.

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

## Part 4 — Test 2: registering for a meeting (~20 minutes)

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
see the word above the dropdown. **This is the interesting one**, and question 2 in
Part 1 was a hint.

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

## Tips

1. **Write tests in UI mode.** `npm run test:ui` shows the DOM at every step and lets
   you try locators against the live page. It will save you most of the time this
   homework costs.
2. **Never sleep.** Locators resolve when used and web-first assertions retry on their
   own. If you reach for `waitForTimeout`, you have not identified the state you are
   waiting for.
3. **Name the test after the behaviour**, not the mechanics. "a visitor can register
   for a published meeting" — not "test register page".
4. **`getByRole` and `getByLabel` first, CSS last.** When you do fall back to CSS,
   write down why. That note is usually a bug report about the markup.
5. **`getByLabel` only works if the markup is correct.** When it cannot find a field
   you can plainly see, suspect the page before you suspect yourself — that is Part 4.
6. **Generate unique data per run** — `` `e2e-${Date.now()}@…` `` — so a rerun does not
   collide with the last one.
7. **When it fails, read the trace before you read the code.** `npm run report`, open
   the failed test, then the trace: a DOM snapshot per step, plus the Network tab that
   shows exactly what the API returned.

---

## Part 5 — Bonus (~10 minutes, optional)

**5a.** Now that the catalogue is fixed, open `/dashboard`. Does "Total Meetings" agree
with what a visitor can see on `/`? Write a test that compares the two screens, then
decide what the correct fix is — there is more than one defensible answer, and choosing
is a human job.

**5b.** Hiding a meeting from the list is not the same as making it non-public. Try
navigating straight to `/meetings/b2000000-0000-0000-0000-000000000005` (the cancelled
meeting). Should that work? Argue either way.

---

## What to bring to the lecture

1. **Your two test files** — `meetings.spec.ts`, `registration.spec.ts`
2. **Your three fixes**, on a branch
3. **Written answers** to the reflection questions in Parts 1, 3 and 4
4. **One sentence** answering this: *how did you know these were the things worth
   testing?* You were told. That is the question the lecture is about.

---

## Summary of deliverables

| #   | Task                                                          | Time   | Required? |
| --- | ------------------------------------------------------------- | ------ | --------- |
| 0   | Setup — run the app, install Playwright, green smoke test      | 10 min | Yes       |
| 1   | Read the code, answer the reflection questions                 | 10 min | Yes       |
| 2   | Playwright in five minutes; try UI mode                        | 5 min  | Yes       |
| 3   | Test 1 — catalogue → **Bug 1** (public list)                   | 15 min | Yes       |
| 4   | Test 2 — registration → **Bug 2a + 2b** (heading, labels)      | 20 min | Yes       |
| 5   | Bonus — dashboard consistency, direct links                    | 10 min | Bonus     |

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
