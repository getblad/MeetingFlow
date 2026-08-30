# Attendee Registration Flow

## Scope

The public attendee registration flow at `/register`: pick a meeting, submit the form, see the
confirmation. Admin pages and admin-only routes are out of scope.

This plan was cut from 6 candidate scenarios to **1 E2E scenario** by asking, for each one, *what
would have to break for this test to fail?* Only failures that require a path the user walks
across screens and processes stayed here. Everything else was pushed down a layer and is recorded
under [Moved to another layer](#moved-to-another-layer) — a rejection is not a deletion of risk.

## Seed file and fixtures

Follows the import/setup pattern in `tests/seed.spec.ts`:

```ts
import { test, expect } from '../fixtures';

test('seed', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Meetings', level: 1 })).toBeVisible();
});
```

The scenario creates a registration, so it must use the `attendee` fixture from `fixtures.ts`
(`{ name, email }`, unique per test via `Date.now()` + `testInfo.workerIndex`) rather than
hardcoded values. The shared SQLite database is never reset between runs, so every run adds a row —
the test must read the "before" count at runtime and never assume a fixed number.

## Preconditions / test data assumptions (observed 2026-08-30 against the running app)

- Web app at http://localhost:5173, API at http://localhost:5062, both must be running.
- Requires at least two **Published** meetings in seed data. Observed: "Product Engineering Meetup"
  (9/9/2026), "Frontend Architecture Summit" (9/25/2026), "Cloud Integration Day" (10/25/2026).
- Meetings are referenced by visible option text, not by ID or index, so the test survives seed
  data being regenerated.

## Locator notes

- Page heading — `getByRole('heading', { name: 'Register for an Meeting', level: 1 })`. The awkward
  "an Meeting" is the real observed copy, not a typo in this plan.
- Register button — `getByRole('button', { name: 'Register' })`.
- Success message — `getByText('Registration created successfully!')`. Content assertion, so
  `getByText` is the right tool.
- Registrations count on the meeting detail page — fully role-based:
  `getByRole('row').filter({ has: getByRole('rowheader', { name: 'Registrations' }) }).getByRole('cell')`.
- Form fields — **`getByLabel` now works.** The three controls this scenario touches were previously
  unreachable by label: each sat beside a `<label>` with the right visible text, but the label had no
  `for`/`id`, did not wrap the control, and the control had no `aria-label`/`aria-labelledby`. That
  was fixed at source in `CreateRegistrationPage.tsx` by adding `htmlFor`/`id` pairs, so **no CSS
  locator is needed anywhere in this plan**:
  - Meeting select → `page.getByLabel('Meeting')`
  - Your Name → `page.getByLabel('Your Name')`
  - Your Email → `page.getByLabel('Your Email')`

  Verified against the running app on 2026-08-30: the accessibility tree reports
  `combobox "Meeting"`, `textbox "Your Name"` and `textbox "Your Email"`.

- **Outstanding:** the Ticket Type select is still unassociated and still exposes no accessible name
  (`combobox` with no name in the a11y tree), so `getByLabel('Ticket Type')` matches nothing. This
  scenario leaves Ticket Type at its default and never locates it, so it is not blocked — but the
  same `htmlFor="ticketType"` / `id="ticketType"` fix should be applied before any test needs that
  control.

## Test Scenarios

### 1. Attendee Registration

**Seed:** `tests/seed.spec.ts`

#### 1.1. Register for a published meeting and see the registration persisted

**File:** `tests/registration/register-for-meeting.spec.ts`

**Why this is E2E:** for it to fail, something has to break in the chain the user actually walks —
the form's own state, the POST to the API, the write to SQLite, and a *different screen* reading
that write back. No single unit, component or endpoint test spans that. This scenario absorbs what
were previously three separate scenarios (happy path, server-side persistence, and "the meeting you
picked is the one you get registered for"); they shared one journey and are cheaper and stricter as
a single test.

**Steps:**
  1. Obtain a unique attendee via the `attendee` fixture.
  2. Navigate to `/` and open a Published meeting's detail page (e.g. "Product Engineering
     Meetup"). Read and store its current Registrations count.
     - expect: a numeric count is captured.
  3. Note the Registrations count of a *second* Published meeting (e.g. "Cloud Integration Day") as
     a control.
  4. Navigate to `/register`.
     - expect: the heading 'Register for an Meeting' (level 1) is visible.
  5. In the Meeting select, choose the first meeting from step 2.
  6. Fill Your Name with `attendee.name` and Your Email with `attendee.email`. Leave Ticket Type at
     its default.
  7. Click 'Register'.
     - expect: 'Registration created successfully!' becomes visible.
  8. Return to the first meeting's detail page and re-read the Registrations count.
     - expect: exactly one greater than the count from step 2 — proving the registration was
       persisted server-side and not merely an optimistic client-side message.
  9. Open the control meeting's detail page from step 3.
     - expect: its count is unchanged — proving the registration was attributed to the meeting the
       user selected and did not leak to another meeting.

## Moved to another layer

- **Meeting dropdown only offers Published meetings** -> component test for
  `CreateRegistrationPage`. (TODO)
  What breaks it: the expression `meetings.filter((ev) => ev.status === "Published")` in
  `CreateRegistrationPage.tsx`. The filter is client-side — the API returns all meetings and the
  component discards the rest. Render the component with a mocked `fetchMeetings` returning
  Published + Draft + Cancelled meetings and assert only the Published ones become `<option>`s. No
  browser round trip needed.
- **Form resets after a successful submit** -> component test for `CreateRegistrationPage`. (TODO)
  What breaks it: the four `setState("")` calls in the `handleSubmit` success branch. One
  component's own state after a mocked-resolved submit.
- **Changing the meeting selection before submit registers the newly-selected meeting** ->
  component test for `CreateRegistrationPage`. (TODO)
  What breaks it: the controlled `<select>`'s `onChange` -> `setMeetingId` -> request payload
  mapping. Assert the mocked `createRegistration` was called with the last-selected `meetingId`.
  The end-to-end half of this risk (the selected id actually reaching the right meeting's row in the
  database) is retained as step 9 of scenario 1.1.
- **Duplicate registration (same attendee, same meeting) is accepted** -> integration test for
  `POST /api/registrations`. (TODO)
  What breaks it: the absence of a uniqueness rule in `RegistrationsEndpoints.cs` — it looks up the
  attendee by email and reuses it, then unconditionally inserts a new `Registration`. This is a rule
  about our endpoint and our database, reachable without a browser.
  **Decide the intended behaviour before writing this test.** The observed behaviour (both
  submissions succeed, count +2) may be the bug rather than the requirement; a test asserting it
  would lock the bug in.
- **Registration is refused for a Draft or Cancelled meeting** -> integration test for
  `POST /api/registrations`. (TODO — newly identified, not in the original plan.)
  What breaks it: `RegistrationsEndpoints.cs` never validates the meeting's status, so a client that
  posts a Draft or Cancelled `MeetingId` directly is accepted. The dropdown filter hides this in the
  UI, which is exactly why no E2E test can catch it — the risk is only reachable below the browser.
- **Confirmation does not survive reload or back-navigation** -> browser-native behaviour, not our
  code. **No test.**
  What breaks it: nothing of ours. `success` is plain `useState`; a reload or remount discards it
  because that is what React and the browser do. Asserting it tests the platform, not MeetingFlow.
  If the confirmation is ever *meant* to survive (a URL param, a redirect to a confirmation route)
  that is a new feature and earns a new E2E scenario at that point.

## NOT VERIFIED

- **NOT VERIFIED: "meeting full" / capacity-based rejection — no such feature exists.** The Meeting
  entity has no capacity concept: `GET /api/meetings/{id}` returns only `id, title, description,
  status, startsAt, endsAt, createdAt, updatedAt, internalNotes, adminOnlyCode, venueId, venue,
  sessions, registrations, feedback`, and the POST endpoint performs no count check. This path
  cannot be constructed against the current data model and is not planned at any layer.
- **NOT VERIFIED: the API-failure path (`setError` and the `.error` div).** The component renders a
  server error message, but no failing submission was observed during exploration, so the actual
  error copy is unknown. If this is worth covering it belongs at component level (mock
  `createRegistration` to reject, assert the message renders) rather than E2E, since forcing a real
  API failure from the browser requires stopping the API or intercepting the route.
- Everything in scenario 1.1 was exercised directly against the running app: dropdown contents,
  submission, the success text, and the Registrations count incrementing on the selected meeting
  while another meeting's count stayed put (observed 95 -> 96, and 147 -> 148 on a second meeting
  while the first held at 97).
