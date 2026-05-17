# MeetingFlow.Microservices

ASP.NET Core minimal-API microservices following the **IDesign method** (Manager / Engine / Resource Accessor), running on Docker Compose with Postgres.

## Architecture

This project intentionally redeclares the same entity classes in every service and ships them as REST request/response bodies across every boundary. There are no shared contracts, no per-endpoint models, and no edge DTOs.

- **src/Gateway/** — public HTTP edge (Client role)
- **src/Managers/MeetingsManager/** — meeting/session/speaker use cases (Manager)
- **src/Managers/RegistrationsManager/** — registration + feedback use cases, inline pricing (Manager)
- **src/Engines/SchedulingEngine/** — pure conflict + capacity logic, no DB (Engine)
- **src/Accessors/DataAccessor/** — EF Core over the meetings / registrations / feedback schemas (Resource Accessor)
- **src/Accessors/NotificationsAccessor/** — notifications schema + fake SMTP gateway (Resource Accessor)
- **infra/postgres/init.sql** — creates four schemas in the shared Postgres container
- **docker-compose.yml** — wires all 7 containers
- **tests/MeetingFlow.Microservices.IntegrationTests/** — xUnit integration tests that fail against the baseline on purpose

## Public REST Endpoints (Gateway, port 8080)

| Method | Path                                            | Description                                          |
| ------ | ----------------------------------------------- | ---------------------------------------------------- |
| GET    | `/meetings`                                     | List meetings (returns full entity graph)            |
| GET    | `/meetings/{id}`                                | Meeting details with nested sessions/registrations   |
| PUT    | `/meetings/{id}`                                | Update meeting (accepts full entity)                 |
| GET    | `/admin/meetings`                               | Admin view, no auth — exposes internal fields        |
| GET    | `/speakers/{id}`                                | Speaker profile including `email` and `phone`        |
| POST   | `/registrations`                                | Create registration (accepts full entity)            |
| GET    | `/registrations/by-meeting/{meetingId}`         | Registrations with full attendee data                |
| POST   | `/feedback`                                     | Submit feedback (accepts full entity)                |

Individual services also expose their own ports for debugging: `5010` (DataAccessor), `5011` (NotificationsAccessor), `5020` (SchedulingEngine), `5030` (MeetingsManager), `5031` (RegistrationsManager).

## Running

```bash
docker compose up --build
```

Postgres starts first (with a healthcheck), then accessors, engine, managers, and gateway. Schemas are created via `infra/postgres/init.sql` and EF `EnsureCreated` produces the tables. Seed data is loaded automatically on first start.

To run the integration tests against the live stack (from the repo root):

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests
```

All 7 tests are expected to fail against the baseline.

## What's Intentionally Wrong

- Each service redeclares its own `Meeting`, `Session`, `Attendee`, `Registration` — drift is silent
- EF Core entities are returned directly from every service, all the way to the public gateway
- Internal fields (`InternalNotes`, `AdminOnlyCode`, `InternalPaymentReference`, `RawPayloadJson`) leak to the public HTTP response
- `POST /registrations` accepts the full entity, letting the client set `paymentStatus` and `internalPaymentReference`
- `SchedulingEngine` and `NotificationsAccessor` receive full entities when they only need a few fields
- `RegistrationsManager` runs inline pricing logic on a full `Meeting` it fetched just for capacity
- The Gateway is a passthrough — no edge models
- `/admin/meetings` is reachable without authentication
- No payload versioning between services
