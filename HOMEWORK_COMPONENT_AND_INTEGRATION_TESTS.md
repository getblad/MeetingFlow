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

Choose one microservice and cover one or two meaningful behaviors with
component tests.

Possible candidates include `SchedulingEngine`, `DataAccessor`, and
`RegistrationsManager`, but the choice and the scenarios are yours.

First identify the selected service's responsibility and choose behaviors that
give useful confidence in that responsibility. Decide which dependencies should
remain real, which may be controlled or replaced, and what observable result
will prove the behavior.

---

## Part 3 — Add one targeted integration test

Choose an integration between two real application components and prove that
they can communicate using their production contract.

Possible boundaries include:

- a producer and consumer communicating through RabbitMQ;
- a Manager communicating with a downstream service over HTTP.

The test should answer one focused question: **are these two components really
compatible?**

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

Use real services and real infrastructure for this flow. Decide how to run the
environment, prepare the scenario, observe the result, and handle the data.

Do not repeat every component-test scenario here. One critical happy path is
enough to prove that the deployed system works together.

---
