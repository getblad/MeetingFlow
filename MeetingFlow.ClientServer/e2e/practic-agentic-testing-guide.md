# Practical Guide: Playwright Test Agents on MeetingFlow.ClientServer (V3)

Step-by-step setup and usage of the three Playwright Test Agents — **planner**,
**generator** and **healer** — on this repository.

Every step lists the command, what it does, and why you run it.

**Target project:** `MeetingFlow.ClientServer` (React on `:5173`, ASP.NET Core API on `:5062`)
**Test project:** `MeetingFlow.ClientServer/e2e`
**Agent host:** Claude Code
**Playwright:** 1.61

**Work on a fresh branch from `main`** (for example `playwright-agents-02`), so the
label bug and the original button name are still in place — both matter for the demo.

Section 10 marks the steps used in the live session. The rest is reference.

**One rule for every prompt in this guide:** keep it short, but never vague. A vague
request gives a wide plan and weak tests. Name the flow, the cap, and the exclusions —
one line each. That is how you would ask a colleague, and it is enough.

---

## 0. What the agents are

Three role definitions that ship with Playwright. Each one is a file: instructions
plus a list of MCP browser tools. One command writes them into your repository, and a
coding agent (Claude Code, VS Code, Codex or OpenCode) runs them.

| Agent | Input | Output |
| --- | --- | --- |
| **planner** | a request, a seed test, optionally a product doc | a Markdown plan in `specs/` |
| **generator** | a plan from `specs/` | Playwright test files in `tests/` |
| **healer** | the name of one failing test | a patch, or a skipped test |

They do not replace `npx playwright test`. The runner still executes the tests.

**How they see the page.** The agents do not take screenshots. They read the
**accessibility tree** — a text snapshot of every element's role, name and state
(saved as `page-*.yml` files while they work). It is the same view a screen reader
gets. This is why an unlabelled input is invisible to both, and why the agents are
cheap on tokens and stable on selectors.

---

## 1. Prerequisites

`init-agents` does **not** install Playwright, does not download a browser, and does
not start your app. All of that is on you, before any agent runs. That is this section.

### 1.1 Start the application

```bash
# terminal 1 — API
cd MeetingFlow.ClientServer/MeetingFlow.Api
dotnet run
```

```bash
# terminal 2 — web
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install && npm run dev
```

**Why:** the agents drive a real browser against a running app.

`playwright.config.ts` has no `webServer` block, so nothing starts these for you. If
you dispatch an agent while the ports are dead, it will spend several turns
discovering that, starting the servers itself, and waiting for them — before it does
any actual work.

Check http://localhost:5173 shows meeting cards before continuing.

> Both servers keep running after an agent finishes. Stop them yourself when done.
> Remember: `dotnet run` does not hot-reload — backend changes need a restart.

### 1.2 Install the test project

```bash
cd MeetingFlow.ClientServer/e2e
npm install
npx playwright install chromium
npm test
```

**What it does:** installs `@playwright/test`, downloads the browser, runs the
existing smoke test.

**Why:** the agents reuse this project — its config, its browser, its conventions.

You should see one passing test. If not, stop and fix that first.

### 1.3 Check versions

```bash
npx playwright --version     # expect 1.61.x
claude --version             # Claude Code must be installed
```

**Why:** the agent definitions are tied to the Playwright version. Version 1.56
introduced the agents, 1.59 added the trace CLI used in step 7.

---

## 2. Install the agent definitions

```bash
cd MeetingFlow.ClientServer/e2e
npx playwright init-agents --loop=claude
```

**What it does:** writes the three agent definitions into the repository. For Claude
Code they land in `.claude/agents/`, plus an MCP server entry in `.mcp.json`.

**Why `--loop=claude`:** the value selects the **host**, not the agent. Valid values
are `vscode`, `claude`, `codex`, `opencode`. There is no `--loop=planner`.

**Honest note:** this one command is not the whole install. In this repository the
full path was: `npm install` and `npx playwright install chromium` first (step 1.2),
then `init-agents`, then the folder and `.gitignore` preparation in step 3. Budget
fifteen minutes, not one command.

### 2.1 Review and commit what it wrote

```bash
git status --short
git diff -- .
```

**Why:** these files are instructions and a tool list for an agent running in your
repository. Read them once. After a Playwright upgrade, re-run the command and read
the diff again — new tools change what the agent is allowed to do.

```bash
git add .claude .mcp.json
git commit -m "chore: playwright test agent definitions (--loop=claude)"
```

**Why a separate commit:** the diff stays readable, and you can revert the
definitions without touching your tests.

### 2.2 Verify the agents are actually wired

`--loop` selects the **host format**, not the terminal you happen to sit in. Claude
Code running inside the VS Code terminal is still Claude Code. Pick `vscode` and you
get `.github/agents/*.agent.md` plus an MCP server that only VS Code's own agent mode
provides — Claude Code cannot see it.

Four checks before you trust a run:

| Check | Correct result |
| --- | --- |
| Where the definitions are | `.claude/agents/*.md` — not `.github/agents/*.agent.md` |
| `/agents` in Claude Code | lists `playwright-test-planner`, `-generator`, `-healer` |
| `/mcp` in Claude Code | the `playwright-test` server is connected |
| Status line during a run | the agent's name — **not** `general-purpose` |

The fastest signal: **you should not need to explain anything about tools in your
prompt.** If you find yourself telling the agent how to open a browser, the wiring is
wrong.

If you installed the wrong loop, re-run in the same folder and remove the old set:

```bash
npx playwright init-agents --loop=claude
git rm -r .github/agents        # only if you do not also use VS Code agent mode
```

Launch Claude Code from the same folder where you ran `init-agents`, or it will not
find `.claude/agents`.

---

## 3. Prepare the inputs

The agents need a working seed test, a place for artifacts, and your rules in writing.

### 3.1 Create the folder layout

```bash
cd MeetingFlow.ClientServer/e2e
mkdir -p specs
```

Result:

```
e2e/
  playwright.config.ts
  fixtures.ts          <- step 3.2
  specs/               <- planner writes here
  tests/
    seed.spec.ts       <- step 3.3
    smoke.spec.ts
```

**Why:** the planner writes plans to `specs/`, the generator writes tests to
`tests/`. Keeping them side by side makes the link between a plan and its test
obvious.

Then add this to `e2e/.gitignore`:

```
.playwright-mcp/
```

**Why:** while an agent explores, the Playwright MCP tools dump one `console-*.log`
and one `page-*.yml` per step into `.playwright-mcp/`. A single run leaves dozens of
files. They are session scratch, regenerated every run, and do not belong in a commit.

The `page-*.yml` files are the accessibility-tree snapshots from section 0 — open one
once. It is what the agent actually reads instead of pixels.

### 3.2 Create `fixtures.ts`

```ts
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
```

**What it does:** gives every test fresh registration data.

**Why it matters for agents:** the generator copies the pattern it sees in the seed
test. If the seed imports `../fixtures`, generated tests import `../fixtures` too. If
the seed hardcodes an email, every generated test will hardcode one.

### 3.3 Create `tests/seed.spec.ts` — and know when to change it

```ts
import { test, expect } from '../fixtures';

test('seed', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Meetings', level: 1 })).toBeVisible();
});
```

```bash
npx playwright test tests/seed.spec.ts
```

**What the seed is for.** The planner **runs** this test before exploring. That
executes your config, fixtures and hooks, so the browser starts in the right state.
The generator uses the file as the example for imports and setup in every generated
test.

**The seed is good as-is when** — like MeetingFlow — the app needs no login, the flow
starts from the home page, and no data setup is needed. Navigate, assert one visible
marker, stop.

**Rewrite the seed when any of these is true:**

- the app requires **login** — the seed must sign in (or load `storageState`) so the
  agents never see a login wall;
- the flow needs a **specific tenant, feature flag or role** — the seed must land there;
- tests need **data reset or seeding** — do it in the seed or a global setup, never
  by hand;
- the start page is not `/` — point the seed at the real starting URL.

**Never let the seed perform the scenario.** If the seed registers an attendee, the
plan learns nothing about registration — and every generated test inherits the noise.
One navigation, one assertion.

### 3.4 Write your rules down: `CLAUDE.md`

At the repository root:

```md
# MeetingFlow — agent context

Read AGENTS.md first. Do not introduce DTO, Request, Response, ViewModel or Mapper
types, and do not refactor the architecture. The flaws are intentional teaching material.

## Testing
- E2E project: MeetingFlow.ClientServer/e2e
- Plans: e2e/specs/    Tests: e2e/tests/    One test file per scenario.
- Follow the imports and setup pattern in e2e/tests/seed.spec.ts
- Prefer getByRole and getByLabel. Justify any CSS locator in a comment.
- Assume unit and component tests cover field validation and formatting.
- App: web http://localhost:5173, API http://localhost:5062
```

**Why:** every rule written here is a sentence you never repeat in a prompt. Without
it the first thing an agent proposes in this repo is the DTO refactor that
`AGENTS.md` forbids — and the planner assumes nothing is covered at any other layer.

### 3.5 Docs before browser

If the project has documentation (README, domain rules, exercise notes), read it
before the planner runs — docs and code are two claims about the same system, and
they disagree in interesting places. Plan from claims you verified in the code; a doc
claim the code never implemented is a documentation fix, not a test. When a plan
scenario comes from a doc, keep the reference: the spec names the doc section, the
test names the spec. This repo's docs are thin — the point stands anyway.

---

## 4. Planner — produce a plan

Open Claude Code in `MeetingFlow.ClientServer/e2e` and give it this request:

```text
Use the planner agent with tests/seed.spec.ts to explore the registration flow
at http://localhost:5173/register.
Plan at most 8 E2E scenarios: pick a meeting, submit the form, see the confirmation.
Skip admin pages and anything a unit or component test should cover.
Save the plan as specs/registration.md and mark anything you could not verify.
```

That is the whole prompt. Short, but each line does work: the seed pins the
environment, the cap stops the 18-scenario flood, the exclusions keep the plan
readable, and the last line makes assumptions visible. Without the cap and the
exclusions this planner returns **18 scenarios** for one form — half of them
validation cases that belong in unit tests.

**What happens:** the agent runs the seed test, opens the app in a browser, walks
the flow reading accessibility snapshots, and writes `specs/registration.md`.

**How long:** minutes, not seconds. It runs as a backgrounded agent, so the terminal
looks idle while it works. Press `ctrl+o` to watch it.

**What to expect in the plan:** concrete steps and concrete expected results — the
same "what to check and what to expect" you were given explicitly in the homework
sheet, now drafted for you. On this branch the planner also reports, on its own, that
the labels are not associated with their inputs — `getByLabel` will not work. That is
the exact bug the homework had you fix by hand. Keep that finding; it drives step 5.

### 4.1 Review the plan

Open `specs/registration.md` and check:

- Does every scenario match something a user actually does?
- Is each expected result observable on screen?
- Does any scenario depend on another one running first?
- Are the assumptions listed?

**Edit it by hand.** The plan is your document. The agent drafted it.

### 4.2 Cut the plan

Even with the cap, expect a scenario or two you do not want. The planner optimises
for coverage of what it can observe and has no cost model — it does not know a test
is a commitment you pay for on every future change.

Cut here. At the plan gate a rejection costs one sentence. After generation it costs
a full code review.

**Which layer does a scenario belong to?** Ask what would have to break for it to fail.

| What breaks it | Layer |
| --- | --- |
| One pure function | Unit |
| One component's rendering or its props | Component |
| The wiring between two of our own parts | Integration |
| A path the user walks across screens and processes | **E2E — keep it** |

Usual candidates to move down: field-validation matrices, default values, formatting
rules, and anything that tests the browser's own behaviour rather than your code.

**Record where each dropped scenario went** — a rejection is not a deletion of risk:

```md
## Moved to another layer
- <scenario> -> unit test for <unit>  (TODO)
- <scenario> -> component test for <component>  (TODO)
- <scenario> -> browser-native behaviour, not our code. No test.
```

**Never assert behaviour you know is wrong.** If a scenario describes a defect (this
API happily returns 201 for a duplicate registration), a generated test would lock
the bug in as a green test. Write the intended behaviour and mark it:

```ts
// Defect: duplicate registration is not rejected by the API
test.fixme('a repeated registration is rejected', async () => { /* ... */ });
```

Target after the cut: **6–8 scenarios**. Enough to demonstrate the whole flow, small
enough to review, run and heal in minutes.

### 4.3 Fix what the save tool did to the file

`planner_save_plan` adds its own numbering and section shape on top of what the
planner wrote. Two things come out wrong, and both matter because the generator
derives `describe` and `test` titles from these headings:

- **Doubled ordinals** — headings like `### 1. 1. Selecting…`. Collapse them.
- **The "not verified" section turned into a test group** — it gets a fake `Seed:`
  and `Steps:` shape. Rewrite it as plain prose with an explicit "do not generate
  tests from this section" line.

### 4.4 Commit

```bash
git add specs/registration.md
git commit -m "test(plan): registration flow"
```

---

## 5. Generator — produce tests

### 5.0 One decision first: fix the markup or not

The plan says the labels are not associated with their inputs — no `htmlFor`, no
wrapping, no `aria-label`. So `getByLabel` and name-based `getByRole` cannot target
the form. That conflicts with the locator rule in `CLAUDE.md`. Two options:

- **Fix the markup first** (the homework fix: `htmlFor` + `id` on the four fields),
  commit it, then generate. Tests come out clean. **Do this.**
- Generate as-is and accept structural CSS locators with a justification comment on
  each. Only when you cannot touch the app.

An interface no screen reader can label is one no agent can target either.

### 5.1 Dispatch the generator

```text
Use the generator agent to implement specs/registration.md.
Follow the pattern in tests/seed.spec.ts. One test file per scenario,
under tests/registration/. Do not add scenarios that are not in the plan.
```

**What happens:** the agent reads the plan, performs each scenario in a real browser
to check its selectors and assertions, and writes the test files.

**Why one file per scenario:** parallel sub-agents writing the same file would
overwrite each other, and the file name then tells you which scenario failed. Nothing
in Playwright requires this — it is a repo convention, so it lives in `CLAUDE.md`
instead of being renegotiated every run. (Consolidating later into grouped files is a
refactor, not a rule. Note it does not buy runtime parallelism here:
`workers: 1` in the config, because of the shared SQLite database.)

**Note on import depth:** files in `tests/registration/` need `'../../fixtures'`,
one level deeper than the seed's `'../fixtures'`. The generator usually handles
this; check it in review.

### 5.2 Speeding up generation

One generator sub-agent doing 8 scenarios works through them serially — minutes each.
Since files are disjoint (one per scenario), parallel dispatch is safe:

```text
Generate tests for specs/registration.md in parallel: one generator sub-agent
per plan group. Files are disjoint, one file per scenario under tests/registration/.
```

Each sub-agent gets its own browser. Three groups in parallel roughly divides the
wall-clock time by three. Do **not** parallelise agents onto the same file — and
remember this is generation-time parallelism only; the test run itself is still
`workers: 1`.

### 5.3 Review the generated code

```bash
npx tsc --noEmit                      # type-checks the tests without running them
npx playwright test tests/registration
```

Every file should start like this — keep these two lines, they are the traceability:

```ts
// spec: specs/registration.md
// seed: tests/seed.spec.ts
import { test, expect } from '../../fixtures';
```

Check four things:

1. **Header** — does it name its spec and seed?
2. **Locators** — `getByRole` and `getByLabel`, not CSS chains. Any `.first()` needs a reason.
3. **Assertions** — do they prove the feature works, or only that the page rendered?
   Watch for near-duplicate tests asserting the same thing — a sign the plan needed
   one more cut.
4. **Data** — does it use the fixture, or a hardcoded email? And no locale-dependent
   strings: option labels here embed `toLocaleDateString()`, so match on the title
   substring, never the formatted date.

Generated tests can contain errors — the Playwright documentation says so directly.
Treat the output as a pull request from a new colleague.

```bash
git add tests/registration
git commit -m "test: registration flow (generated, reviewed)"
```

---

## 6. Run the suite

```bash
npm test                 # headless
npm run test:ui          # interactive, best while working
npm run report           # open the last HTML report
```

**Why this matters:** from here the runner is in charge. No model is involved. This
is what runs in CI.

---

## 7. Healer — repair a failing test

The healer takes the name of one failing test, replays it, inspects the current page,
proposes a patch and re-runs it.

### 7.1 Create a failure — small on purpose

For the demo, change the confirmation message in
`MeetingFlow.Web/src/pages/CreateRegistrationPage.tsx`:

```diff
-        Registration created successfully!
+        You are registered!
```

Vite hot-reloads, so no restart. Run **only the affected tests** — a full suite of
failures burns the locator timeout on every one:

```bash
npx playwright test tests/registration --reporter=list
```

Expect **one or two tests** to fail — the ones that assert the confirmation text.
That is the point: a small break keeps the diagnosis readable and the healer fast.

> **Why not rename the Register button?** It works, but every submitting test locates
> that button, so one rename fails most of the suite at once — and the host agent
> then spends 5–10 minutes re-investigating all of them before dispatching the
> healer. Big breaks make good chaos and bad demos.

### 7.2 Look at the evidence first

```bash
npm run report
```

Open the failed test, then the trace. You get a DOM snapshot per step and the network
log. The config sets `trace: "retain-on-failure"`, which is what makes this possible.

Read the failure like this: the error names the locator and what it waited for; the
trace shows what the page actually contained at that step. Here the snapshot shows
the new text `You are registered!` where the assertion expected the old one — cause
found in under a minute, before any agent runs.

From the command line (Playwright 1.59+):

```bash
npx playwright trace open test-results/<folder>/trace.zip
```

**Why look before healing:** you must know *why* it failed before you let anything
change the test. The classification in section 8 is your decision, not the healer's.

### 7.3 Run the healer

You already know the cause, so say so — otherwise the host agent spends minutes
re-discovering it:

```text
The test "<failing test title>" fails because the confirmation text changed —
an intentional UI change. Skip the investigation and dispatch the healer agent
directly: minimal patch, re-run only that test. Do not weaken assertions or add skips.
```

**What happens:** the healer replays the test, reads the current page, updates the
expected text, re-runs until green.

### 7.4 Review the patch

Ask one question before accepting:

> Before this patch the test proved X. After this patch it proves Y. Are X and Y the
> same?

Here they are: same flow, same confirmation, different wording. Accept it.

If they differ, it is not a repair. It is a change of intent, and someone has to
approve it.

---

## 8. When not to use the healer

The healer will try to fix any failing test you point it at. Only one kind of failure
is its job — and **you classify, not the agent**. The host agent helps you gather
evidence (the trace, `git diff`, the spec, the docs), but the verdict is a human
decision, because it decides what changes: the product, the plan, the test, or the
environment.

| The failure is | What you see | What to do | Healer? |
| --- | --- | --- | --- |
| A product bug | the control or result is missing; app error | fix the product | **no** |
| An intentional change | requirement and UI changed together | update the plan, then the test | after approval |
| Test drift | locator or data is stale, behaviour is correct | repair the test | **yes** |
| Environment | app down, wrong flags, expired account | fix the environment, re-run | usually no |

How to tell them apart quickly: `git diff` on the app — if the app changed, it is
row 1 or 2, and the spec (and docs, when you have them) says which: behaviour still
matches the requirement → intentional change; it no longer does → product bug. App
unchanged and the page still does the right thing → test drift. App unchanged and the
page is broken everywhere → environment.

A healer pointed at a product bug will find a way to make the suite green — and the
bug stays in the product. A skipped test is not a repair; it is coverage you stopped
running.

---

## 9. CI boundary

CI runs committed tests. **The agents never run in the pipeline.**

```yaml
- run: npm ci
- run: npx playwright install --with-deps chromium
- run: npx playwright test
```

**Why:** generation and healing are source changes and belong in a branch with a
reviewer. If your pipeline needs a model to produce a test result, the result is not
reproducible.

---

## 10. What to demonstrate in the session

Total **6 minutes**. Everything is already committed before you start — you are
showing finished work and running one agent.

### Before the session

1. Steps 1–7.1 completed on the demo branch; **one or two tests failing** from the
   confirmation-text change already verified once (then reverted, ready to redo live).
2. Both servers running, `npx playwright test tests/registration` green.
3. Claude Code open in `MeetingFlow.ClientServer/e2e`.
4. Terminal font 18pt or larger.
5. `CreateRegistrationPage.tsx` open in the editor at the confirmation message.
6. A prepared healer diff saved, in case the live run stalls.
7. A `page-*.yml` from `.playwright-mcp/` handy for the co-lecturer question.

### The six beats

| # | Time | What you do | What you say |
| --- | --- | --- | --- |
| 1 | 0:00–0:45 | `ls .claude/agents/` | Three definitions. Generated by one command, not written by me. |
| 2 | 0:45–1:45 | Open `specs/registration.md` | The plan. Concrete steps, concrete expected results — what the homework sheet gave you, drafted by the agent. It found the label bug on its own. |
| 3 | 1:45–2:30 | Open a test, point at the header | Two comments link the test back to the plan and the seed. |
| 4 | 2:30–3:00 | `git log --oneline` | Definitions, plan, markup fix, tests. Each step reviewable on its own. |
| 5 | 3:00–4:00 | Change the message, run the affected tests → 1–2 red | Small break on purpose. Read the failure, glance at the trace: expected old text, page has new text. |
| 6 | 4:00–6:00 | Healer prompt (with the diagnosis), show the patch | I told it what I already know, so it goes straight to work. Same proof, new wording. Accepted. |

### While the healer runs (planted question from the co-lecturer)

> *"How do the agents actually see the page — do they take screenshots?"*

Answer with the `page-*.yml` file open: no — an accessibility-tree snapshot per step:
role, name, state of every element. Same thing a screen reader gets, cheaper than
pixels, and stable to point at. Then the two callbacks: that is why the planner found
the label bug on its own — the same bug you fixed by hand in the homework — and that
is why the fix made the app testable for humans, screen readers and agents at once.

### Fallback

If the healer stalls past 90 seconds: stop it, open the diff you prepared earlier,
and say what it would have done. The point is the review step, not the wall clock.

### What not to demonstrate

- `init-agents` — one command, nothing to watch.
- A full planner or generator run — minutes of idle terminal. Show their **output**.

---

## 11. Troubleshooting

| Symptom | Cause |
| --- | --- |
| Agent times out opening the page | Vite is not on 5173, or the API is not on 5062 |
| Agent spends its first turns starting servers | you dispatched it before the app was up |
| Agent proposes a DTO refactor | `CLAUDE.md` / `AGENTS.md` not in context |
| Generated test hardcodes an email | your seed test hardcodes one — fix the seed, regenerate |
| The plan has 15+ scenarios | your request had no cap — see the prompt in step 4 |
| Host agent "thinks" for minutes before healing | you gave it no diagnosis — see step 7.3 |
| Whole suite fails after one edit | the break was too big — see step 7.1 |
| `--loop=claude` rejected | wrong Playwright version; check `npx playwright --version` |
| No trace in the report | `trace` is not `retain-on-failure` in `playwright.config.ts` |
| Backend change has no effect | `dotnet run` does not hot-reload — restart it |
| Dozens of untracked files appear | `.playwright-mcp/` — add it to `.gitignore` (step 3.1) |
| Test names contain doubled numbers | you generated from an uncleaned plan — see step 4.3 |
| `getByLabel` finds nothing on `/register` | labels not associated with inputs — see step 5.0 |
| Status line says `general-purpose` | the agent definitions are not wired — see step 2.2 |

---

## 12. Links

- Playwright Test Agents — https://playwright.dev/docs/test-agents
- Planner / generator / healer guide — https://qaskills.sh/blog/playwright-test-agents-planner-generator-healer
- Claude Code sub-agent practices — https://www.pubnub.com/blog/best-practices-for-claude-code-sub-agents/
- Trace viewer — https://playwright.dev/docs/trace-viewer
