# Attendee Registration Flow

## Scope

Covers the public attendee registration flow at `/register`: pick a meeting, submit the registration form, see the confirmation. Admin pages and admin-only routes are explicitly out of scope and are not exercised by any scenario below. Field-level validation (required-field markers, email/phone format, string/date formatting, character limits) is assumed to be covered by unit/component tests and is intentionally excluded here.

## Seed file and fixtures

All tests follow the import/setup pattern in `tests/seed.spec.ts`:

```ts
import { test, expect } from '../fixtures';

test('seed', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Meetings', level: 1 })).toBeVisible();
});
```

Every scenario that **creates** a registration must use the `attendee` fixture from `fixtures.ts` (`{ name, email }`, unique per test via `Date.now()` + `testInfo.workerIndex`) instead of hardcoded name/email values, so repeated test runs never collide on duplicate data.

## Preconditions / test data assumptions (observed on 2026-08-30 against the running app)

- Web app at http://localhost:5173, API at http://localhost:5062, both confirmed running.
- The home page (`/`) listed 5 seeded meetings: "Product Engineering Meetup" (Published), "Frontend Architecture Summit" (Published), "Cloud Integration Day" (Published), "Distributed Systems Workshop" (Draft), "AI Tools for Developers" (Cancelled).
- The `/register` Meeting dropdown only offered the 3 Published meetings as options: "Product Engineering Meetup (9/9/2026)", "Frontend Architecture Summit (9/25/2026)", "Cloud Integration Day (10/25/2026)", plus a placeholder "-- Select Meeting --". Draft/Cancelled meetings were absent.
- Ticket Type select options observed: "General" (default selected), "VIP", "Early Bird", "Student".
- These are current seed-data facts, not guarantees; scenarios reference meetings by their visible option text rather than by ID/index so they keep working if seed data is regenerated, as long as the same shape (>=1 Published meeting, >=1 Draft, >=1 Cancelled) holds. If future seed data changes this shape, Scenario 2 in particular will need updated meeting names.

## Locator notes (read before writing tests)

- Register button — has a proper accessible name: `page.getByRole('button', { name: 'Register' })`.
- Page heading — level-1 heading with the verbatim (slightly awkward, but that's the real observed copy) text "Register for an Meeting": `page.getByRole('heading', { name: 'Register for an Meeting', level: 1 })`.
- Success message — a `<p>` with the exact observed text `Registration created successfully!`, rendered above the form fields after a successful submit. Per convention this is a content assertion, so `getByText` is appropriate: `page.getByText('Registration created successfully!')`.
- **Confirmed accessibility gap on the form fields.** DOM inspection (`document.querySelectorAll('label, select, input')`) showed that the Meeting select, Your Name input, Your Email input, and Ticket Type select each sit next to a `<label>` with the correct visible text, but that label has no `for`/`id` attribute, does not wrap the control, and the control has no `aria-label`/`aria-labelledby`. Result: `getByLabel('Meeting')`, `getByLabel('Your Name')`, `getByLabel('Your Email')`, and `getByLabel('Ticket Type')` all match nothing. This is a real product accessibility defect (worth a separate bug report), not a test-authoring gap. Until it's fixed, use these fallback locators, each justified individually since plain `getByRole(...,{name})` can't work without an accessible name:
  - Meeting select → `page.locator('select[required]')` (CSS locator). Justification: no accessible name/label exists; this is the only `<select>` on the page carrying the `required` attribute, so it's identified by a real semantic HTML attribute, not by position.
  - Your Name input → `page.locator('input[type="text"]')` (CSS locator). Justification: no accessible name/label exists; `type="text"` is the only text input on the page, distinguishing it from the email input by attribute rather than DOM order.
  - Your Email input → `page.locator('input[type="email"]')` (CSS locator). Same justification as above, using `type="email"`.
  - Ticket Type select → `page.getByRole('combobox').last()` (role locator using position). Justification: no accessible name/label exists, and unlike the Meeting select it has no distinguishing attribute (it is the one `<select>` without `required`); since the form always renders exactly two comboboxes with Ticket Type consistently second in DOM order, `.last()` is the least brittle option remaining that still uses `getByRole`.
- Meeting detail page "Registrations" count (used to verify server-side persistence) — fully role-based, no CSS needed: `page.getByRole('row').filter({ has: page.getByRole('rowheader', { name: 'Registrations' }) }).getByRole('cell')`.
- Meeting detail page heading — `page.getByRole('heading', { level: 1 })` shows the meeting's title (e.g. "Product Engineering Meetup"), useful to confirm you landed on the right meeting.

## NOT VERIFIED items

- **NOT VERIFIED: "meeting full" / capacity-based server rejection.** Reason: the Meeting API entity has no capacity/maxAttendees concept at all — inspecting the live API response for a meeting (`GET /api/meetings/{id}`) returned only these keys: `id, title, description, status, startsAt, endsAt, createdAt, updatedAt, internalNotes, adminOnlyCode, venueId, venue, sessions, registrations, feedback`. With no capacity field in the data model, a "meeting full" rejection path cannot be constructed against the current app and is not included as a scenario below, per instructions to record and move on rather than speculate.
- No other NOT VERIFIED items. Every other scenario, locator, and behavioral claim in this plan (including that duplicate registrations are currently accepted rather than rejected, that the confirmation does not survive reload/back-navigation, and that switching the meeting selection before submit registers the newly-selected meeting) was exercised directly against the running app during plan authoring.

## Test Scenarios

### 1. Attendee Registration

**Seed:** `tests/seed.spec.ts`

#### 1.1. Happy path — register for a published meeting

**File:** `tests/registration/happy-path.spec.ts`

**Steps:**
  1. Obtain a unique attendee via the `attendee` fixture ({ name, email }).
  2. Navigate to /register.
    - expect: The heading 'Register for an Meeting' (level 1) is visible.
  3. In the Meeting select (page.locator('select[required]')), choose 'Product Engineering Meetup (9/9/2026)'.
    - expect: The option is selected.
  4. Fill Your Name (page.locator('input[type="text"]')) with attendee.name.
  5. Fill Your Email (page.locator('input[type="email"]')) with attendee.email.
  6. Leave Ticket Type at its default value and click the 'Register' button (getByRole('button', { name: 'Register' })).
    - expect: The text 'Registration created successfully!' becomes visible.
    - expect: The Meeting select resets to '-- Select Meeting --' and the Name/Email inputs are cleared back to empty, confirming the form fully resets after a successful submit.

#### 1.2. Meeting dropdown only offers Published meetings

**File:** `tests/registration/meeting-dropdown-published-only.spec.ts`

**Steps:**
  1. Navigate to / and note the full list of meeting titles with their status badges (Published / Draft / Cancelled).
    - expect: At least one Published, one Draft, and one Cancelled meeting are present in seed data (observed: 'Distributed Systems Workshop' = Draft, 'AI Tools for Developers' = Cancelled).
  2. Navigate to /register and read all option labels of the Meeting select (page.locator('select[required]')).
    - expect: Every Published meeting title from the home page appears as an option (matched by visible text, e.g. 'Product Engineering Meetup (9/9/2026)').
    - expect: Neither 'Distributed Systems Workshop' nor 'AI Tools for Developers' (the Draft and Cancelled meetings) appears in the option list.
    - expect: The only non-meeting entry is the placeholder '-- Select Meeting --'.

#### 1.3. Successful registration is persisted server-side

**File:** `tests/registration/persists-server-side.spec.ts`

**Steps:**
  1. From the Meetings list (/), open a Published meeting's detail page and read the current 'Registrations' count via getByRole('row').filter({ has: getByRole('rowheader', { name: 'Registrations' }) }).getByRole('cell').
    - expect: A numeric count is captured, e.g. observed '95' for Product Engineering Meetup at one point during exploration.
  2. Navigate to /register, select that same meeting, and submit a registration using the attendee fixture (per the happy-path steps).
    - expect: The success message 'Registration created successfully!' appears.
  3. Navigate back to the same meeting's detail page and re-read the 'Registrations' count.
    - expect: The new count is exactly one greater than the count captured before submitting, confirming the registration was persisted server-side rather than being only an optimistic client-side message. (Observed directly: 95 -> 96 for Product Engineering Meetup.)

#### 1.4. Confirmation does not survive reload or back-navigation

**File:** `tests/registration/confirmation-not-persisted.spec.ts`

**Steps:**
  1. On /register, submit a successful registration using the attendee fixture (per the happy-path steps).
    - expect: The success message 'Registration created successfully!' is visible.
  2. Reload the page (e.g. page.reload() or re-navigate to /register).
    - expect: The success message is no longer visible, and the Meeting/Name/Email fields are back to their empty/default state. (Observed directly.)
  3. Repeat the flow: submit a new successful registration with a fresh attendee, then navigate away via the 'Meetings' nav link, then use the browser back button to return to /register.
    - expect: Same outcome as the reload check: the success message is gone and the form is back to its pristine empty state. This confirms the confirmation is transient in-memory component state — it is not persisted via URL, query string, or storage, and resets on any remount. (Observed directly.)

#### 1.5. Changing meeting selection before submit registers the currently-selected meeting

**File:** `tests/registration/meeting-selection-switch.spec.ts`

**Steps:**
  1. Record the current 'Registrations' count for two different Published meetings, A ('Product Engineering Meetup') and B ('Cloud Integration Day'), via each meeting's detail page (same locator as the persistence scenario).
  2. Navigate to /register, select meeting A in the Meeting select, then change the selection to meeting B without submitting.
    - expect: The Meeting select shows meeting B as selected.
  3. Fill Name/Email using the attendee fixture, leave Ticket Type at its default, and click 'Register'.
    - expect: The success message 'Registration created successfully!' appears.
  4. Re-check both meetings' 'Registrations' counts.
    - expect: Meeting B's count increases by exactly one.
    - expect: Meeting A's count is unchanged. (Observed directly: switching from 'Product Engineering Meetup' to 'Cloud Integration Day' before submit resulted in Cloud Integration Day going 147 -> 148 while Product Engineering Meetup stayed at 97.)

#### 1.6. Duplicate registration (same attendee, same meeting) is currently accepted, not rejected

**File:** `tests/registration/duplicate-registration-accepted.spec.ts`

**Steps:**
  1. Navigate to /register, select a Published meeting, and submit a registration using the attendee fixture.
    - expect: The success message 'Registration created successfully!' appears.
  2. Immediately submit a second registration for the same meeting, reusing the exact same attendee.name and attendee.email from the first submission.
    - expect: The second submission also shows 'Registration created successfully!' — no duplicate/conflict error is surfaced. (Observed directly: no rejection occurred.)
    - expect: The meeting's 'Registrations' count (per the persistence scenario's technique) increases by two in total across both submissions, confirming both were persisted as separate entries. (Observed directly: count went 96 -> 97 after the second, identical submission.)
