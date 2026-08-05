# Homework: Component and Integration Tests

> **Goal:** Explore the MeetingFlow microservice architecture, decide where the
> important test boundaries are, and implement a small test pyramid. The task
> describes behavior to verify, but you choose the test level, dependencies,
> tools, and environment lifecycle.

---

## Part 0 — Make sure the system works

Start the microservice application:

```bash
cd MeetingFlow.Microservices
docker compose up --build
```

Verify that:

- Gateway is healthy at `http://localhost:8080/health`;
- `GET http://localhost:8080/meetings` returns data;
- PostgreSQL, RabbitMQ, and the backend services are running.

When you finish exploring, press `Ctrl+C` in the terminal where Docker Compose
is running. This stops the services.

If you also want to remove the stopped containers and the Compose network, run:

```bash
docker compose down
```

---

## Part 1 — Design the test strategy first

Review the architecture in `MeetingFlow.Microservices/README.md` and inspect
the `Gateway`, `Managers`, `Engines`, and `Accessors` folders.

Identify:

- the public boundary of the complete backend;
- the boundary of each individual microservice;
- synchronous HTTP dependencies;
- asynchronous messaging dependencies;
- infrastructure owned by or required by each service.

Before writing tests, complete this small table:

| Area | Proposed test level | Entry point | What should be real? | What can be replaced? |
| --- | --- | --- | --- | --- |
| Scheduling rules |  |  |  |  |
| Data persistence |  |  |  |  |
| Registration orchestration |  |  |  |  |
| Notification delivery |  |  |  |  |
| Complete registration flow |  |  |  |  |

There may be several valid answers. Be ready to explain why your chosen
boundary provides enough confidence without starting unnecessary parts of the
system.

---

## Part 2 — Add component tests

Choose at least two microservices and cover them with component tests.

Possible candidates:

### SchedulingEngine

Test observable scheduling behavior, for example:

- overlapping sessions in the same room;
- adjacent sessions that should not conflict;
- invalid time ranges;
- capacity calculation and invalid capacity input.

At least one group of similar cases should be implemented as an xUnit
`Theory`, not as several duplicated `Fact` methods.

### DataAccessor

Test persistence behavior, for example:

- reading an existing meeting with related data;
- creating a registration and reading it back;
- requesting data that does not exist;
- ensuring internal model fields do not leak through HTTP.

Decide what kind of database gives the test meaningful confidence and how its
data should be isolated and cleaned up.

### RegistrationsManager

Test registration orchestration, for example:

- successful registration;
- an attendee who is already registered;
- a meeting with no available capacity;
- ensuring persistence and event publication do not happen after a rejected
  operation.

Think about every dependency of the Manager. Decide which dependencies need to
be real for these scenarios, which can be controlled, and how you will observe
outgoing calls and events.

### Component-test expectations

For each selected service:

- enter through its public HTTP boundary;
- verify observable behavior rather than private methods;
- keep tests deterministic;
- document real and replaced dependencies;
- make sure one test cannot influence another.

---

## Part 3 — Add one targeted integration test

Choose an integration between two real application components and prove that
they can communicate using their production contract.

Possible boundaries include:

- a producer and consumer communicating through RabbitMQ;
- a Manager communicating with a downstream service over HTTP.

The test should answer one focused question: **are these two components really
compatible?**

Consider:

- contract serialization and deserialization;
- routing or endpoint configuration;
- required infrastructure;
- readiness before the test action starts;
- bounded waiting for asynchronous results;
- cleanup after success or failure.

Do not start the complete MeetingFlow system for this test. Components unrelated
to the selected integration should remain outside its boundary.

---

## Part 4 — Add one backend system test

Cover the complete registration flow through the public backend boundary:

```text
Gateway → RegistrationsManager → DataAccessor → PostgreSQL
                           ├→ SchedulingEngine
                           └→ RabbitMQ → NotificationsAccessor → PostgreSQL
```

The test should prove that:

1. a registration can be created through Gateway;
2. the saved registration can be read again;
3. the registration notification is eventually created.

Use real services and real infrastructure for this flow.

The environment must start automatically. Do not require participants or CI to
run `docker compose up` manually before the test, and do not start the complete
microservice topology from an xUnit fixture.

Choose an external orchestration approach, such as a dedicated test script,
task, or CI step. It should:

- start the environment;
- wait until the required services are ready;
- run the system test;
- collect useful logs when something fails;
- remove the test environment and data afterward.

Think about how the system test can run without conflicting with an already
running local MeetingFlow environment.

Do not repeat every component-test scenario here. One critical happy path is
enough to prove that the deployed system works together.

---

## What to submit

1. The completed strategy table from Part 1.
2. Component tests for at least two services, including at least one `Theory`.
3. One targeted integration test between real components.
4. One automated backend system test.
5. A short `TESTING.md` with:
   - commands for each test level;
   - Docker requirements;
   - real and replaced dependencies;
   - environment startup and cleanup behavior.

---

## Summary

| Part | Task |
| --- | --- |
| 0 | Run and inspect the system |
| 1 | Design the test strategy |
| 2 | Add component tests |
| 3 | Test one real integration |
| 4 | Test the complete backend flow |
