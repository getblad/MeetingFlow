# MeetingFlow microservices tests

This directory contains examples for the components and integration tests
lecture. The examples are added incrementally so each test boundary remains
visible.

Planned test-suite boundaries:

- `MeetingFlow.SchedulingEngine.ComponentTests` — starts the complete
  SchedulingEngine HTTP application in the test process with
  `WebApplicationFactory`.
- `MeetingFlow.IntegrationTests` — a specific integration between real components.

## SchedulingEngine component tests

Install/restore the test dependencies and run the project from the repository
root:

```bash
dotnet restore MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
```

`WebApplicationFactory<Program>` boots the real Minimal API with an in-memory
test server. Requests still pass through ASP.NET Core routing, JSON
serialization, model binding, validation endpoints and response serialization,
but no TCP port, Docker container or external service is required.

## DataAccessor component tests

These tests start two real parts:

1. DataAccessor runs in the test process through `WebApplicationFactory`.
2. PostgreSQL 16 runs in a disposable Docker container through
   `Testcontainers.PostgreSql`.

Prerequisites:

- Docker Desktop must be running;
- no local PostgreSQL instance or fixed host port is required.

Run from the repository root:

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj
```

The xUnit fixture starts PostgreSQL once for the test class, injects its dynamic
connection string as `POSTGRES_CONN`, and then creates the HTTP client. The
normal DataAccessor startup code creates the EF Core schema and seed data in
that database. After the class finishes, Testcontainers removes the container
and its data.

## RegistrationsManager component tests

The real RegistrationsManager runs through `WebApplicationFactory`. Its external
dependencies are controlled test doubles:

- WireMock.Net HTTP stub for DataAccessor;
- WireMock.Net HTTP stub for SchedulingEngine;
- in-memory spy implementing `IEventPublisher` instead of RabbitMQ;
- fixed `TimeProvider` for deterministic pricing.

Run from the repository root (Docker is not required):

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.RegistrationsManager.ComponentTests/MeetingFlow.RegistrationsManager.ComponentTests.csproj
```

The stubs return only responses configured by each scenario. An unexpected
downstream call receives no successful stub response and fails the test. This
lets the suite verify Manager orchestration and early exits without starting
DataAccessor, SchedulingEngine, PostgreSQL or RabbitMQ.

## Registration notification integration test

This targeted integration test verifies one asynchronous boundary rather than
the complete application flow:

```text
real EventPublisher
  -> RabbitMQ Testcontainer
    -> real NotificationsAccessor consumer
      -> PostgreSQL Testcontainer
```

It covers the production exchange, routing key, queue binding, JSON event
contract, consumer and notification persistence. Gateway, Manager endpoints,
DataAccessor and SchedulingEngine are not started.

Docker Desktop must be running. Run from the repository root:

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj
```

The fixture waits until RabbitMQ reports an active consumer before publishing.
After publishing, the test polls the NotificationsAccessor HTTP API with a
bounded timeout because message delivery is asynchronous. Fixed sleeps are not
used for synchronization.

## Backend system integration test

The system test creates its prerequisites and executes the business flow through
the public Gateway API:

```text
test -> Gateway -> RegistrationsManager -> DataAccessor -> PostgreSQL
                       |-> SchedulingEngine
                       `-> RabbitMQ -> NotificationsAccessor -> PostgreSQL
```

The test uses the complete backend started locally with Docker Compose. The
`SystemIntegrationFixture` connects to Gateway at `http://localhost:8080` and
NotificationsAccessor at `http://localhost:5011`, verifies their health
endpoints, waits for the local RabbitMQ consumer and creates `HttpClient`
instances. It does not start or stop Docker.

Before running the system test, start the backend with the system-test override.

In the first terminal, open the `MeetingFlow.Microservices` directory and run:

```bash
cd MeetingFlow.Microservices
docker compose -f docker-compose.yml -f docker-compose.system-tests.yml up --build
```

This is the same local Compose environment, ports and volumes. The override only
sets `TestSupport__Enabled=true` for DataAccessor and NotificationsAccessor.
Without that explicit setting, the `/_test/...` cleanup routes are not mapped at
all. They are also never forwarded by Gateway.

Wait until the services are ready and keep that terminal running. In a second
terminal, from the repository root, run the system test:

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj --filter Category=System
```

After the backend is ready, the same test can be run or debugged directly from
the VS Code Testing view. It creates a unique venue, meeting and attendee through
public Gateway endpoints, then creates and verifies the registration. Cleanup
deletes the notification and registration through opt-in, Accessor-owned test
support routes, followed by the public attendee, meeting and venue DELETE
endpoints. The test never connects to PostgreSQL directly and never depends on
seed data or execution order. Existing local data is neither required nor
cleared between runs.

Run only the targeted RabbitMQ integration test:

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj --filter Category=Integration
```

Docker Desktop and Docker Compose are required for the system test. In CI or a
larger project, the same startup, readiness, test and cleanup commands can be
wrapped in a script or workflow step.
