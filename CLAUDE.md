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