# Unit Testing Lecture — Practical Examples

Teaching examples demonstrating how application design affects unit testing,
built on the existing MeetingFlow domain.

## Lecture materials

| File | What it is |
|---|---|
| `unit-testing-lecture-meetingflow.pptx` | 16-slide deck. Every code sample is taken from this repository, so the slide and the editor show the same example. |
| `UNIT_TESTING_LECTURE_SPEECH.md` | 30–40 minute speaker script keyed to the slides, with file/line references and timing sheet. |
| this file | Directory map, commands, and exercise instructions. |

### How the examples relate to the shipped application

Be precise about this when teaching — students will open the real files.

- **The badge ternary is real and triplicated.** The same Published/Draft/Cancelled ternary
  appears in `MeetingCard.tsx:9`, `MeetingTable.tsx:23`, and `MeetingDetailsPage.tsx:29`.
  Extracting `resolveMeetingRow` removes a genuine duplication.
- **The `canPublish` permission rule is new.** No shipped component has it; it is introduced
  by the lecture to give the resolver a decision with more than one input.
- **`RegistrationsEndpoints.cs` has no rules yet.** It performs no status, capacity, or
  existence check, and it receives its `DbContext` by injection. It shares exactly one problem
  with the "before" service: ambient `DateTimeOffset.UtcNow` (line 30). `HardToTestRegistrationService`
  is what that handler becomes once the rules are added inline.
- **The API is EF Core on SQLite** (`Program.cs:9`), not Cosmos.

## Directory structure

```
MeetingFlow.ClientServer/
├── MeetingFlow.Web/
│   ├── vitest.config.ts                              # test config (solution tests)
│   ├── vitest.config.exercise.ts                     # test config (exercise tests)
│   ├── vitest.setup.ts                               # jest-dom matcher setup
│   └── src/examples/unit-testing-lecture/
│       ├── types.ts                                   # MeetingStatus, UserRole
│       ├── before/MeetingRow.tsx                      # ❌ hard-to-test component
│       ├── after/resolveMeetingRow.ts                 # ✅ pure resolver
│       ├── after/MeetingRow.tsx                       # ✅ thin component
│       ├── __tests__/resolveMeetingRow.test.ts        # ✅ table-driven unit tests
│       ├── __tests__/MeetingRow.test.tsx              # ✅ component tests (RTL)
│       └── exercise/                                  # ✏️ student exercises
│           ├── resolveMeetingRow.exercise.ts
│           ├── MeetingRow.exercise.tsx
│           └── __tests__/
│               ├── resolveMeetingRow.exercise.test.ts
│               └── MeetingRow.exercise.test.tsx
│
└── MeetingFlow.Api.Tests/
    └── UnitTestingLecture/
        ├── Before/HardToTestRegistrationService.cs    # ❌ hard-to-test service
        ├── Refactored/
        │   ├── MeetingInfo.cs                         # input record
        │   ├── RegistrationDecision.cs                # result union
        │   ├── RegistrationRule.cs                    # ✅ pure validation rule
        │   ├── IMeetingRepository.cs                  # repository interface
        │   ├── INotificationGateway.cs                # notification interface
        │   ├── RegistrationResult.cs                  # service result union
        │   └── RegistrationService.cs                 # ✅ testable orchestration
        ├── Tests/
        │   ├── RegistrationRuleTests.cs               # ✅ parameterized rule tests
        │   └── RegistrationServiceTests.cs            # ✅ orchestration tests
        └── Exercise/                                  # ✏️ student exercises
            ├── Types.cs
            ├── RegistrationRule.cs
            ├── RegistrationServiceExercise.cs
            └── Tests/
                ├── RegistrationRuleExerciseTests.cs
                └── RegistrationServiceExerciseTests.cs
```

## Mapping: before → refactored → tested

| Before (hard to test)                       | Refactored                              | Tests                          |
|---------------------------------------------|-----------------------------------------|--------------------------------|
| `before/MeetingRow.tsx` — inline badge logic, fetches data internally, permission check in JSX | `after/resolveMeetingRow.ts` — pure function | `resolveMeetingRow.test.ts` — table-driven, no React |
| (same component)                            | `after/MeetingRow.tsx` — thin, props-driven  | `MeetingRow.test.tsx` — RTL component tests |
| `Before/HardToTestRegistrationService.cs` — `new HttpClient()`, `DateTimeOffset.UtcNow`, inline validation | `Refactored/RegistrationRule.cs` — pure rule | `RegistrationRuleTests.cs` — parameterized |
| (same service)                              | `Refactored/RegistrationService.cs` — injected deps, `TimeProvider` | `RegistrationServiceTests.cs` — fakes + `FakeTimeProvider` |

## Commands

### Frontend tests

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install
npm test                  # solution tests only
npm run test:exercise     # exercise tests (skipped until implemented)
```

### Backend tests

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Api.Tests
dotnet test                                             # all tests
dotnet test --filter "FullyQualifiedName~Tests."        # solution tests only
dotnet test --filter "FullyQualifiedName~Exercise."     # exercise tests only
```

### Type checking / build

```bash
# Frontend
cd MeetingFlow.ClientServer/MeetingFlow.Web
npx tsc --noEmit

# Backend
dotnet build MeetingFlow.ClientServer/MeetingFlow.Api.Tests
```

## Suggested live-demonstration order

1. **Show the problem** — open `before/MeetingRow.tsx` and `Before/HardToTestRegistrationService.cs`.
   Ask: "How would you unit-test the badge logic? The permission check? The registration validation?"

2. **Extract the pure function (frontend)** — walk through `resolveMeetingRow.ts`.
   Run `resolveMeetingRow.test.ts` to show how fast and readable pure-function tests are.

3. **Component test** — show `MeetingRow.test.tsx`.
   Highlight: `getByRole`, `userEvent.click`, asserting visible output — not CSS classes or state.

4. **Extract the pure rule (backend)** — walk through `RegistrationRule.cs`.
   Run `RegistrationRuleTests.cs` — parameterized, no mocks, instant.

5. **Testable orchestration** — show `RegistrationService.cs` vs `HardToTestRegistrationService.cs`.
   Run `RegistrationServiceTests.cs` — hand-written spies, `FakeTimeProvider`, AAA pattern.

6. **Exercise** — students complete the TODOs (~10–15 minutes).

## Test boundary reference

| Behavior                                      | Correct test type         |
|-----------------------------------------------|---------------------------|
| Badge variant for a given status              | Unit test (resolver)       |
| "Can this user publish?" permission decision  | Unit test (resolver)       |
| Visible Publish button and click interaction  | React component test (RTL) |
| Registration allowed / rejected decision      | Unit test (rule)           |
| Rejected registrations are never notified     | Unit test (service + spy)  |
| Real DB query, JSON serialization, routing    | Integration test           |
| DI container wiring, middleware pipeline      | Integration test           |

Integration tests (real DB, HTTP pipeline) are intentionally NOT part of this lecture.
The existing `RegistrationsEndpoints.cs` endpoint is the natural candidate for a future
`WebApplicationFactory`-based integration test.

## Exercise instructions

### React (3 tasks, ~8 minutes)

1. **Implement the resolver** — open `exercise/resolveMeetingRow.exercise.ts`, replace the
   `throw` with the status/permission logic. Refer to the rules in the comment.

2. **Add missing test case** — open `exercise/__tests__/resolveMeetingRow.exercise.test.ts`,
   uncomment and complete the "Draft + Viewer → cannot publish" case.

3. **Add component test** — open `exercise/__tests__/MeetingRow.exercise.test.tsx`,
   uncomment and complete the "Viewer cannot see Publish button" test.

4. Remove `describe.skip` → `describe` and run `npm run test:exercise`.

### C# (3 tasks, ~8 minutes)

1. **Implement the rule** — open `Exercise/RegistrationRule.cs`, replace the `throw` with
   status and capacity checks. Refer to the rules in the comment.

2. **Inject TimeProvider** — open `Exercise/RegistrationServiceExercise.cs`, replace
   `DateTimeOffset.UtcNow` with `_timeProvider.GetUtcNow()`.

3. **Add the missing test** — open `Exercise/Tests/RegistrationServiceExerciseTests.cs`,
   uncomment and complete the "rejected registration does not send notification" test.

4. Remove `Skip = "..."` from the `[Fact]` attributes and run `dotnet test`.

### Solutions

The completed implementations are in the `after/` (frontend) and `Refactored/` + `Tests/`
(backend) directories. The instructor can show these after the exercise.

## Expected test results

### Frontend (`npm test`)

```
✓ resolveMeetingRow
  ✓ Draft + Admin + canManage → can publish
  ✓ Draft + Organizer + canManage → can publish
  ✓ Draft + Viewer + canManage → cannot publish
  ✓ Draft + Admin without canManage → cannot publish
  ✓ Published → shows registrations, cannot publish
  ✓ Cancelled → no actions available

✓ MeetingRow
  ✓ shows Publish button for an authorized user with a draft meeting
  ✓ does not show Publish button for a Viewer
  ✓ shows publishing state and calls onPublish after clicking Publish
  ✓ shows registration count for a published meeting
```

### Backend (`dotnet test`)

```
✓ RegistrationRuleTests.Validate_returns_correct_decision (4 cases)
✓ RegistrationServiceTests.Valid_registration_is_timestamped_saved_and_notified
✓ RegistrationServiceTests.Rejected_registration_is_not_saved
✓ RegistrationServiceTests.Rejected_registration_does_not_send_notification
✓ RegistrationServiceTests.Missing_meeting_returns_not_found
✓ RegistrationServiceTests.CancellationToken_is_forwarded_to_repository
```
