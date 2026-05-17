# MeetingFlow.Microservices — IDesign Teaching Baseline

The third sibling example in the **MeetingFlow** teaching repository, alongside
[MeetingFlow.Monolith](../MeetingFlow.Monolith) and
[MeetingFlow.ClientServer](../MeetingFlow.ClientServer).

It is an intentionally-bad **microservices** version of the same domain, structured
along the **IDesign method** (Manager / Engine / Resource Accessor), so students can
see what bad DTO design looks like once the boundaries are real network hops between
services.

> **This baseline exposes EF Core / domain entities across every service boundary on
> purpose.** Service-A's local class is sent over HTTP to Service-B, which deserializes
> it into its own near-duplicate copy. Internal fields like `InternalNotes`,
> `AdminOnlyCode`, `InternalPaymentReference`, and `RawPayloadJson` cross every wire.
> This is **not** recommended production architecture — it is the teaching baseline.

---

## Architecture

Five services + a gateway + a single Postgres container.

```
                       ┌──────────────────┐
                       │   API Gateway    │   ← public HTTP edge (port 8080)
                       └────────┬─────────┘
                                │ REST
              ┌─────────────────┼──────────────────┐
              v                                    v
   ┌────────────────────┐              ┌────────────────────────┐
   │  MeetingsManager   │              │ RegistrationsManager   │   ← Managers
   │   (port 5030)      │              │      (port 5031)       │     (use cases)
   └─────┬──────────┬───┘              └────┬─────────┬─────────┘
         │ REST     │ REST                  │ REST    │ REST
         v          │                       │         v
   ┌──────────┐    │                       │   ┌──────────────────┐
   │Scheduling│    │                       │   │ NotificationsAcc │   ← Accessor
   │  Engine  │    │                       │   │   (port 5011)    │     (notifications +
   │ (5020)   │    │                       │   │                  │      fake SMTP)
   └──────────┘    │                       │   └────────┬─────────┘
                   v                       v            │
                ┌──────────────────────────────────┐    │
                │      DataAccessor (5010)         │    │  ← Accessor
                │  meetings / registrations /      │    │    (per-schema repos)
                │  feedback repositories           │    │
                └────────┬─────────────────────────┘    │
                         │ EF Core (Npgsql)             │ EF Core (Npgsql)
                         v                              v
                ┌────────────────────────────────────────────┐
                │     Postgres (port 5432)                   │
                │  schemas: meetings, registrations,         │
                │           feedback, notifications          │
                └────────────────────────────────────────────┘
```

### Services and their IDesign role

| # | Service                       | Role            | What it does                                                                          |
|---|-------------------------------|-----------------|---------------------------------------------------------------------------------------|
| 1 | `Gateway`                     | Client          | Public REST edge on port 8080. Forwards to managers. No business logic.               |
| 2 | `MeetingsManager.Api`         | Manager         | Use cases: list/get/update meetings, sessions, admin views, speakers.                 |
| 3 | `RegistrationsManager.Api`    | Manager         | Use cases: register attendee, submit feedback. Inline ticket-pricing logic.           |
| 4 | `SchedulingEngine.Api`        | Engine          | Pure logic: session time-window conflict checks, meeting capacity check. No DB.       |
| 5 | `DataAccessor.Api`            | Resource Accessor | EF Core over the meetings / registrations / feedback schemas. CRUD over REST.       |
| 6 | `NotificationsAccessor.Api`   | Resource Accessor | Owns the notifications schema AND the fake SMTP gateway. CRUD + `send` over REST.   |

### IDesign call rules (enforced in code)

- Gateway → Manager ✅
- Manager → Engine and Manager → Accessor ✅
- Manager → Manager: **forbidden** (cross-use-case data flows through DataAccessor)
- Engine → anything: **forbidden** (engines are pure functions over their request body)
- Accessor → other services: **forbidden**

### Compromises for tractability (the plan was max 5 services)

- Pricing logic is **inlined** into `RegistrationsManager` instead of being a separate
  `PricingEngine.Api`. See [InlineTicketPricing.cs](src/Managers/RegistrationsManager.Api/Pricing/InlineTicketPricing.cs).
  Extracting it is the bonus refactor.
- `DataAccessor.Api` **co-deploys three repositories** (meetings, registrations, feedback)
  instead of being three separate accessor services. The layering lesson survives via
  the per-schema repository classes in
  [src/Accessors/DataAccessor.Api/Repositories/](src/Accessors/DataAccessor.Api/Repositories/).
- `NotificationsAccessor.Api` is split out because notifications has a second resource —
  the SMTP gateway — which is the classic IDesign justification for a dedicated accessor.

---

## How to Run

From this directory:

```bash
docker compose up --build
```

Postgres comes up first (with a healthcheck), then the accessors, then the engine,
then the managers, then the gateway. Schemas are created via
[infra/postgres/init.sql](infra/postgres/init.sql) and EF `EnsureCreated` produces
the tables on first start. Seed data is loaded automatically.

Once everything is up:

| URL                                           | What it returns                                              |
|-----------------------------------------------|--------------------------------------------------------------|
| `http://localhost:8080/health`                | Gateway health                                               |
| `http://localhost:8080/meetings`              | All meetings, full entity graph (intentional)                |
| `http://localhost:8080/meetings/{id}`         | Meeting with sessions, registrations, attendees, feedback    |
| `http://localhost:8080/admin/meetings`        | Admin view — same payload, no auth (intentional)             |
| `http://localhost:8080/speakers`              | Speakers including `Email`, `Phone`, `InternalNotes`         |
| `http://localhost:8080/registrations/by-meeting/{meetingId}` | Registrations for a meeting                   |
| `POST http://localhost:8080/registrations`    | Create a registration. Body is the full `Registration` entity. |
| `POST http://localhost:8080/feedback`         | Submit feedback. Accepts full `Feedback` entity.             |

Individual services are reachable on their own ports for debugging:
`5010` (DataAccessor), `5011` (NotificationsAccessor), `5020` (SchedulingEngine),
`5030` (MeetingsManager), `5031` (RegistrationsManager).

### Building without Docker

Each project is a normal .NET 10 Web SDK project. From this directory:

```bash
dotnet build MeetingFlow.Microservices.slnx
```

To run a single service locally, set the `POSTGRES_CONN` and the upstream service URLs
via environment variables (see the defaults at the top of each `Program.cs`).

---

## What Is Intentionally Wrong

These are the smells students will find and refactor. **Do not "fix" them by default.**

1. **No contracts library.** Every service redeclares its own `Meeting`, `Session`,
   `Attendee`, `Registration`. Near-duplicates that drift silently.
2. **One class is the EF entity AND the REST request body AND the REST response body.**
   The same `Meeting` flows DB → DataAccessor JSON → Manager → Gateway → public HTTP,
   carrying `InternalNotes`, `AdminOnlyCode`, audit fields, and nav properties the whole way.
3. **POST/PUT endpoints accept the full entity.** `POST /registrations` accepts a complete
   `Registration` body, including server-controlled fields like `PaymentStatus` and
   `InternalPaymentReference`. A client can supply any value it likes.
4. **Engine and Accessor calls are over-fetched.**
   - `SchedulingEngine.check-conflict` receives the whole `Session` entity when it only
     needs `(StartsAt, EndsAt, RoomName)`.
   - `NotificationsAccessor./notifications/send` receives the whole `Attendee` plus the
     whole `Meeting` when it only needs `(toEmail, subject, body)`.
   - `RegistrationsManager`'s inline pricing operates on a full `Meeting` it just fetched.
5. **Accessors return entity graphs.** `GET /data/meetings/{id}` uses
   `.Include(Sessions).ThenInclude(Speaker).Include(Registrations).ThenInclude(Attendee)`.
   `GET /notifications` returns full `Notification` entities including `RawPayloadJson`.
6. **No payload versioning.** No `v1` namespace, no schema-version field. Field renames
   break consumers silently.
7. **Typed HttpClients hide contract drift.** Each `*Client` deserializes upstream JSON
   into its own local class — drift is invisible until a field nulls out.
8. **Internal IDs and audit timestamps leak** all the way to the public gateway response.
9. **Gateway is a passthrough.** No edge models — public callers get the raw internal shapes.
10. **Cross-manager data fetched via Accessor with the same fat shape.**
    `RegistrationsManager` reads the full `Meeting` graph from `DataAccessor` just to
    check venue capacity for the registration use case.

---

## Tech Stack

| Layer       | Technology                                       |
|-------------|--------------------------------------------------|
| Services    | ASP.NET Core 10 minimal APIs                     |
| Persistence | EF Core 9 + Npgsql (Postgres 16, schema per BC)  |
| Transport   | REST only (`HttpClient` + `System.Net.Http.Json`) |
| Orchestration | Docker Compose                                 |

---

## Folder layout

```
MeetingFlow.Microservices/
├── README.md
├── MeetingFlow_MICROSERVICES_TEAM_EXERCISE.md
├── MeetingFlow.Microservices.slnx
├── docker-compose.yml
├── infra/postgres/init.sql
└── src/
    ├── Gateway/                          (port 8080, Client edge)
    ├── Managers/
    │   ├── MeetingsManager.Api/          (port 5030, Manager)
    │   └── RegistrationsManager.Api/     (port 5031, Manager; inline pricing inside)
    ├── Engines/
    │   └── SchedulingEngine.Api/         (port 5020, Engine)
    └── Accessors/
        ├── DataAccessor.Api/             (port 5010, Accessor; meetings/registrations/feedback)
        └── NotificationsAccessor.Api/    (port 5011, Accessor; notifications + fake SMTP)
```

See [MeetingFlow_MICROSERVICES_TEAM_EXERCISE.md](MeetingFlow_MICROSERVICES_TEAM_EXERCISE.md)
for the student exercises.
