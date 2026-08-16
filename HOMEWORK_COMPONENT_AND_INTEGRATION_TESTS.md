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

Before writing tests, think about how you would fill out this table:

| Area | Proposed test level | Entry point | What should be real? | What can be replaced? |
| --- | --- | --- | --- | --- |
| Scheduling rules |  |  |  |  |
| Data persistence |  |  |  |  |
| Registration orchestration |  |  |  |  |
| Notification delivery |  |  |  |  |
| Complete registration flow |  |  |  |  |

---

## Part 2 — Add component tests

Choose at least two microservices and cover them with component tests.

Possible candidates include `SchedulingEngine`, `DataAccessor`, and
`RegistrationsManager`, but the choice and the scenarios are yours.

For each selected service, first identify its responsibility and choose a small
set of behaviors that gives useful confidence in that responsibility. Decide
which dependencies should remain real, which may be controlled or replaced,
and what observable result will prove the behavior.

Choose `Fact`, `Theory`, or another suitable test form based on the scenarios
you selected. The test form should make the intent clearer rather than satisfy
an artificial requirement.

If a selected component works with persistent data, decide what kind of database
provides meaningful confidence and how the data will be isolated and cleaned
up. Each test should own the data it asserts on and must not depend on production
seed data or test execution order.

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

Use real services and real infrastructure for this flow. Start the backend
locally with Docker Compose, then run or debug the test from your test runner or
IDE. The test fixture should connect to the running system rather than starting
the complete topology itself.

Think about readiness, test-data ownership, and repeated runs. The test should
not depend on an empty database or fixed seed records, and it must not modify
data it does not own. Decide how prerequisites are created and cleaned up
without exposing unnecessary production operations solely for test convenience.
Choose and justify an approach that keeps repeated runs isolated and leaves no
test-owned data behind.

As an optional design question, consider how the local sequence could later be
automated by a script or CI step so that startup, readiness checks, test
execution, logs and cleanup do not require separate manual commands.

Do not repeat every component-test scenario here. One critical happy path is
enough to prove that the deployed system works together.

---