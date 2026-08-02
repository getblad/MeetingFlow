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
