# MeetingFlow — end-to-end tests

Playwright tests that drive the React app in a real browser against the real API.

## Prerequisites

Both servers must be running:

```bash
# terminal 1
cd MeetingFlow.ClientServer/MeetingFlow.Api
dotnet run                     # http://localhost:5062

# terminal 2
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install && npm run dev     # http://localhost:5173
```

## Install

```bash
cd MeetingFlow.ClientServer/e2e
npm install
npx playwright install chromium
```

## Run

```bash
npm test              # headless, list + HTML reporter
npm run test:ui       # interactive UI mode — best for writing tests
npm run test:headed   # watch the browser do it
npm run report        # open the HTML report of the last run
```

## Layout

```
e2e/
├── playwright.config.ts   # baseURL, reporters, trace policy
├── tests/
│   └── smoke.spec.ts      # worked example — should pass
└── package.json
```

See `HOMEWORK_PRE_LECTURE_E2E.md` in the repository root for the exercise.
