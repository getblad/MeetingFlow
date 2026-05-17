# MeetingFlow Microservices Team Exercise

## Team Scope

You are the **Microservices Team**.

Your projects are:

```text
MeetingFlow.Microservices/
  src/
    Gateway/
    Managers/
      MeetingsManager/
      RegistrationsManager/
    Engines/
      SchedulingEngine/
    Accessors/
      DataAccessor/
      NotificationsAccessor/
```

You are working on a distributed system following the **IDesign method**: a public
Gateway, two Managers (use-case orchestrators), one Engine (pure logic), and two
Resource Accessors (data + external SMTP).

The goal is to understand how exposing EF Core / domain entities across **service
boundaries** couples every service to every other service.

---

## Purpose of This Exercise

This repository is a teaching project.

The baseline architecture is intentionally imperfect.

Each service redeclares its own copy of every entity. The same `Meeting` shape (with
`InternalNotes`, `AdminOnlyCode`, full nav properties) flows:

```text
Postgres
  ↓
EF Core entity (DataAccessor)
  ↓
REST JSON to MeetingsManager → deserialized into a near-duplicate local class
  ↓
REST JSON to Gateway → deserialized into yet another near-duplicate
  ↓
public HTTP response on port 8080
```

Same pattern for `Registration`, `Attendee`, `Session`, `Notification`.

This is not recommended production architecture. It is intentionally written this way
so your team can identify the problems and refactor them later.

---

## Project Theme

The project is called **MeetingFlow**.

It is a small meeting/workshop management system.

The domain includes:

- Meetings
- Sessions
- Speakers
- Attendees
- Registrations
- Venues
- Feedback
- Notifications

Same domain as the [MeetingFlow.Monolith](../MeetingFlow.Monolith) and
[MeetingFlow.ClientServer](../MeetingFlow.ClientServer) examples, so you can compare
how the same smells show up in three different architectures.

---

## Your Projects

| Service                       | Role            | Port | Notes                                       |
|-------------------------------|-----------------|------|---------------------------------------------|
| `Gateway`                     | Client (edge)   | 8080 | Public HTTP edge. Forwards to managers.     |
| `MeetingsManager`         | Manager         | 5030 | Meeting/session/speaker/admin use cases.    |
| `RegistrationsManager`    | Manager         | 5031 | Registration + feedback use cases.          |
| `SchedulingEngine`        | Engine          | 5020 | Pure conflict + capacity logic.             |
| `DataAccessor`            | Resource Accessor | 5010 | Postgres I/O. Three repositories.         |
| `NotificationsAccessor`   | Resource Accessor | 5011 | Notifications schema + fake SMTP.         |

---

## Important Rule

Do not start by rewriting the whole system.

The goal is to refactor gradually.

Each task should focus on **one boundary** between two services, **one endpoint**, or
**one use case**.

For example, do not refactor every service's `Meeting` class at once. Start with the
public gateway boundary, because it is the most visible.

---

# What Is Intentionally Wrong?

The project intentionally contains several architectural problems.

You should identify and discuss them before fixing them.

---

## 1. Each service redeclares its own copy of every entity

There is no shared contracts library. `Meeting` is declared four times:

```text
src/Accessors/DataAccessor/Models/Meeting.cs
src/Managers/MeetingsManager/Models/Meeting.cs
src/Managers/RegistrationsManager/Models/Meeting.cs
src/Gateway/Models/Meeting.cs
```

They look the same today. They will not stay that way.

If `DataAccessor`'s `Meeting.Title` is renamed to `Name`, `MeetingsManager`'s deserialized
copy silently nulls out `Title` with no compile error and no runtime error.

---

## 2. The EF Core entity IS the REST request body AND the REST response body

`DataAccessor` returns `Meeting` directly from EF. `MeetingsManager` deserializes
it into its own `Meeting`, hands it to the Gateway, and the Gateway hands it to the public
caller — internal fields and nav properties included:

```text
InternalNotes
AdminOnlyCode
Sessions[].InternalNotes
Registrations[].InternalPaymentReference
Venue.InternalContactName / InternalContactPhone
```

---

## 3. POST endpoints accept full entities

`POST /registrations` on the gateway forwards the body to `RegistrationsManager`, which
accepts a full `Registration` body. The client controls fields it should not:

```text
PaymentStatus
InternalPaymentReference
RegisteredAt
Id
```

Try it:

```bash
curl -X POST http://localhost:8080/registrations \
  -H "Content-Type: application/json" \
  -d '{
    "id":"00000000-0000-0000-0000-000000000001",
    "meetingId":"b2000000-0000-0000-0000-000000000001",
    "attendeeId":"e5000000-0000-0000-0000-000000000010",
    "ticketType":"VIP",
    "paymentStatus":"Paid",
    "internalPaymentReference":"HACKED-REF-123"
  }'
```

The hacked reference will land in the database as-is.

---

## 4. Engine and Accessor calls receive over-fetched payloads

| Caller                        | Callee                                | What's sent                                   | What's actually needed                        |
|-------------------------------|---------------------------------------|-----------------------------------------------|-----------------------------------------------|
| `MeetingsManager`             | `SchedulingEngine.check-conflict`     | Full `Session` candidate + full `Session[]`   | `(StartsAt, EndsAt, RoomName)` per session    |
| `RegistrationsManager`        | `SchedulingEngine.check-capacity`     | Full `Meeting` entity                         | `(venueCapacity, currentRegistrationCount)`   |
| `RegistrationsManager`        | `NotificationsAccessor./send`         | Full `Attendee` + full `Meeting`              | `(toEmail, subject, body)`                    |
| `RegistrationsManager` (self) | `InlineTicketPricing.CalculatePrice`  | Full `Meeting` + full `Registration`          | `(ticketType, status, startsAt)`              |

Inspect the request bodies in `docker compose logs notifications-accessor` after a POST
to `/registrations` to see this for real.

---

## 5. Accessors return entity graphs

`GET /data/meetings/{id}` uses
`.Include(Sessions).ThenInclude(Speaker).Include(Registrations).ThenInclude(Attendee)`,
and the entire graph (including `InternalNotes` on every entity, `Email`/`Phone` on
attendees, `InternalContactName` on the venue) is serialized and sent to the manager.

`GET /notifications` returns full `Notification` entities including the `RawPayloadJson`
field that carries provider-specific telemetry.

---

## 6. No payload versioning

No `v1` namespace, no `$schema` field, no content negotiation. If the request shape
needs to change, every caller breaks at the same time.

---

## 7. Typed HttpClients hide the contract drift

```csharp
public async Task<Meeting?> GetMeetingAsync(Guid id)
    => await _http.GetFromJsonAsync<Meeting>($"/data/meetings/{id}");
```

The returned `Meeting` is the manager's local class. The accessor's `Meeting` is a
different class with the same shape. The type signature implies a contract that does
not actually exist.

---

## 8. The Gateway is a passthrough

There is no edge model. The Gateway has its own `Meeting` class, but for several
endpoints it does not even deserialize — it just streams the upstream response body
back to the public caller. The public API contract is whatever the manager happens to
return today.

---

# Main Learning Goal

The main idea is:

```text
In a microservices system, every service-to-service boundary needs its own contract.
EF entities are not contracts. Internal classes are not contracts. Contracts are
explicit, versioned, and owned by the boundary.
```

Before defining a payload that crosses a service boundary, ask:

```text
Who owns this payload — the producer or the consumer?
What version is it?
What is the MINIMUM shape that lets the consumer do its job?
Can this payload safely evolve without coordinated deploys?
Does this payload expose data the consumer is not allowed to see?
```

---

# Required Tasks for the Microservices Team

## Task 1: Explore the baseline

```bash
docker compose up --build
```

Then call the endpoints listed in the [README](README.md#how-to-run).

### Deliverable

Write a short note answering:

```text
Which fields appear in the public gateway response that should never leave the backend?
Which payloads are over-fetched (sent more data than the receiver actually uses)?
Which classes are redeclared in more than one service, and how likely are they to drift?
```

---

## Task 2: Map the entity duplication

Find every place `Meeting` (or `Session`, `Attendee`, `Registration`) is declared.

### Deliverable

A table:

```text
| Class    | Files declaring it                  | Differences today | Drift risk |
|----------|-------------------------------------|-------------------|------------|
| Meeting  | (list 4 files)                      |                   |            |
| Session  | (list files)                        |                   |            |
```

---

## Task 3: Introduce a contracts library

Create a new project `MeetingFlow.Microservices.Contracts` referenced by all services.

It should contain endpoint-specific request and response **records** — not EF entities.

Start with the smallest useful one: the **public meeting list**.

Replace the four `Meeting` classes used on that path with one shared response record
that contains only what the public meeting list actually needs:

```text
id
title
startsAt
endsAt
status
venueName
venueCity
```

### Deliverable

Show before/after diagrams for the `/meetings` request path.

---

## Task 4: Add a Gateway edge layer

The public gateway should never return internal fields, even by accident.

Introduce edge request/response models in the Gateway that are explicitly the public
contract. Anything not listed in the edge model must not appear in the response.

### Deliverable

Pick two endpoints (`GET /meetings/{id}` and `GET /admin/meetings`) and show the new
edge model. Explain why public and admin edges must be separate.

---

## Task 5: Narrow the Engine call

Refactor `MeetingsManager → SchedulingEngine.check-conflict` so the engine receives
only what it needs.

Define a small request shape (`StartsAt`, `EndsAt`, `RoomName`, plus a list of the
same shape for existing sessions). Update the engine to accept it.

### Deliverable

Explain why the engine should not depend on the manager's domain model at all.

---

## Task 6: Narrow the Accessor call

Refactor `RegistrationsManager → NotificationsAccessor./notifications/send` so the
accessor receives `(toEmail, subject, body, channel)` instead of full `Attendee` + full
`Meeting`.

### Deliverable

Explain what the accessor is now allowed to assume about the caller, and what it is
no longer allowed to know about the meeting domain.

---

## Task 7: Lock down `POST /registrations`

The gateway should accept a narrow request shape (`meetingId`, `attendeeId`,
`ticketType`) — never the full `Registration` entity.

The server controls:

```text
id
registeredAt
paymentStatus
internalPaymentReference
```

### Deliverable

Demonstrate that the curl from "What Is Intentionally Wrong #3" can no longer set
`InternalPaymentReference` after the refactor.

---

## Task 8: Version the cross-service payloads

Once your contracts library exists, version it. Move records into a `V1` namespace and
add a `$version` field (or a header). Show how a `V2` record can coexist with `V1` for
a deployment window.

### Deliverable

Explain how the producer and consumer can deploy independently if the payloads are
versioned.

---

# Optional Advanced Tasks

## Task 9: Extract the inline pricing into a `PricingEngine`

Today `RegistrationsManager` runs ticket-pricing logic inline against the full `Meeting`
+ `Registration` entities. Pull this into a proper `PricingEngine` service with a
narrow request shape:

```text
ticketType
meetingStatus
daysUntilMeeting
```

### Deliverable

Explain why this is the kind of logic that belongs in an Engine, and what changes when
it stops touching the persistence shape.

---

## Task 10: Compare contract strategies

In a microservices system, what are the tradeoffs between:

- a single shared contracts library (Task 3)
- per-service-owned contracts published over OpenAPI
- generated clients (e.g. NSwag/Kiota)
- async event contracts on a message bus
- consumer-driven contracts (Pact)

### Deliverable

A short comparison table covering ownership, drift risk, deploy coordination, and
upgrade cost.

---

## Task 11: Security audit of cross-service payloads

Find every field that should never leave its bounded context and write a removal plan:

```text
InternalNotes               (on Meeting, Session, Speaker, Attendee)
AdminOnlyCode               (on Meeting)
InternalPaymentReference    (on Registration)
RawPayloadJson              (on Notification)
TechnicalDetails            (on AuditLogEntry)
ModerationNotes             (on Feedback)
InternalContactName/Phone   (on Venue)
Email, Phone                (on Speaker, in public contexts)
```

### Deliverable

For each field: which service owns it, which services currently see it, and which
should be allowed to see it after the refactor.

---

# Suggested Refactoring Order

```text
1. Contracts library + public /meetings (Task 3)
2. Gateway edge layer for /meetings/{id} and /admin/meetings (Task 4)
3. Narrow SchedulingEngine.check-conflict request (Task 5)
4. Narrow NotificationsAccessor./send request (Task 6)
5. Narrow POST /registrations request (Task 7)
6. Version the contracts (Task 8)
7. Pricing engine extraction (Task 9)
8. Strategy comparison + security audit (Tasks 10–11)
```

This order starts with the most visible boundary (the public gateway) and works
inward toward the engine and accessor calls.

---

# Questions to Ask During the Exercise

```text
1. Which service OWNS this payload's shape?
2. Is this payload at a system boundary, or internal to one bounded context?
3. What is the minimum the receiver needs?
4. Is this data public, admin-only, or internal?
5. Who is allowed to set this field on the wire?
6. Can the producer and consumer evolve independently, or must they deploy together?
7. Should this be a synchronous request/response, or an asynchronous event?
8. Does this payload allow a caller to do something they should not be able to do?
```

---

# Expected Final Outcome for the Microservices Team

By the end of the exercise, you should understand:

- why every service-to-service hop is a contract, not a function call
- why redeclared near-duplicate classes are a silent-drift hazard
- why over-fetching at service boundaries is worse than over-fetching at API boundaries
- the difference between a Manager's internal model, an Accessor's persistence model,
  an Engine's pure-function input, and a Gateway's edge model
- the tradeoffs between shared contracts libraries, OpenAPI-generated contracts,
  versioned payloads, and async event contracts

You do not need to refactor every service perfectly.

The goal is to understand the boundaries and make intentional architectural decisions.
