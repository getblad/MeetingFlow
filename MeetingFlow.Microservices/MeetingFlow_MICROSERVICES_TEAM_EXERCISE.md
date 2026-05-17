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
Gateway, two Managers (use cases), one Engine (pure logic), and two Resource
Accessors (database, notifications + SMTP).

The goal is to understand how using the same model classes across every
service boundary couples the database, the managers, the gateway, and the public
HTTP contract together.

---

## Purpose of This Exercise

This repository is a teaching project.

The baseline architecture is intentionally imperfect.

The microservices project intentionally uses the same model classes everywhere.
Each service redeclares its own near-duplicate copy of every entity, and the
same shape flows from the database through the accessor, through the manager,
through the gateway, and out to the public HTTP response.

That means the same model is used for:

```text
Database row (Postgres)
  ↓
EF Core entity in the Accessor
  ↓
REST JSON response from the Accessor
  ↓
Local class in the Manager
  ↓
REST JSON response from the Manager
  ↓
Local class in the Gateway
  ↓
Public HTTP response on port 8080
```

This is not recommended production architecture.

It is intentionally written this way so your team can identify the problems and
refactor them into proper service contracts later.

---

## Project Theme

The project is called **MeetingFlow**.

It is a small meeting/workshop management system.

The domain includes:

- Meetings
- Sessions
- Speakers
- Participants
- Registrations
- Venues
- Feedback
- Notifications

Same domain as the **MeetingFlow.Monolith** and **MeetingFlow.ClientServer** examples,
so you can compare how the same smells show up in three different architectures.

The same model is used in many different places.

For example, the `Meeting` model is used as:

- the EF Core entity inside `DataAccessor`
- the JSON response body returned by `DataAccessor`
- a local class inside `MeetingsManager`
- a local class inside `RegistrationsManager` (a second, near-duplicate copy)
- the JSON response body returned by the Manager
- a local class inside `Gateway`
- the public HTTP response on the gateway

This is the main thing you need to notice.

Different services need different things from a `Meeting`, but the baseline
project uses one large model everywhere.

---

## Your Projects

```text
MeetingFlow/
  MeetingFlow.Microservices/
    src/
      Gateway/                          (public edge, port 8080)
      Managers/
        MeetingsManager/                (port 5030)
        RegistrationsManager/           (port 5031)
      Engines/
        SchedulingEngine/               (port 5020)
      Accessors/
        DataAccessor/                   (port 5010, Postgres)
        NotificationsAccessor/          (port 5011, Postgres + fake SMTP)
```

The microservices project uses:

- ASP.NET Core minimal APIs
- EF Core
- Postgres (one container, four schemas)
- REST between services
- Docker Compose

No React is needed for your team.

---

## Important Rule

Do not start by rewriting the whole system.

The goal is to refactor gradually.

Each task should focus on one boundary between two services, one endpoint, or
one use case.

For example, do not refactor every service's `Meeting` class at once.

---

# What Is Intentionally Wrong?

The project intentionally contains several architectural problems.

You should identify and discuss them before fixing them.

---

## 1. The same model is declared in every service

There is no shared contracts library.

The `Meeting` class is declared four separate times:

```text
src/Accessors/DataAccessor/Models/Meeting.cs
src/Managers/MeetingsManager/Models/Meeting.cs
src/Managers/RegistrationsManager/Models/Meeting.cs
src/Gateway/Models/Meeting.cs
```

They look the same today. They will not stay that way.

If one service renames `Title` to `Name`, the other services silently null out
the field with no compile error.

---

## 2. EF Core entities are returned directly from every service

`DataAccessor` returns the EF Core `Meeting` entity straight from the database.

`MeetingsManager` deserializes it into its own near-duplicate `Meeting` and
returns it as-is.

`Gateway` returns whatever the manager returned, also as-is.

The same shape ends up in the public HTTP response on port 8080.

This means consumers of the public API depend on the persistence model.

If the database model changes, the public API can break.

---

## 3. Public and internal data are mixed together

The same entities contain fields like:

```text
InternalNotes
AdminOnlyCode
InternalPaymentReference
InternalContactName
InternalContactPhone
ModerationNotes
TechnicalDetails
RawPayloadJson
```

These fields may be useful internally, but they should not automatically appear
in public API responses on the gateway.

Today they do.

---

## 4. POST endpoints accept full entities

The `POST /registrations` endpoint on the gateway forwards the body to
`RegistrationsManager`, which accepts a full `Registration` entity.

This is risky because the client can set fields that should only be controlled
by the server.

Examples:

```text
payment status
internal payment reference
registration id
registeredAt
```

Try it:

```text
curl -X POST http://localhost:8080/registrations
  -H "Content-Type: application/json"
  -d '{
    "id":"99999999-9999-9999-9999-000000000001",
    "meetingId":"b2000000-0000-0000-0000-000000000001",
    "attendeeId":"e5000000-0000-0000-0000-000000000015",
    "ticketType":"VIP",
    "paymentStatus":"Paid",
    "internalPaymentReference":"HACKED-REF-123"
  }'
```

The "HACKED-REF-123" string lands in the database as-is.

---

## 5. Engine and Accessor calls receive over-fetched payloads

A service call should send only what the receiver needs.

The baseline sends entire entities instead.

Examples:

```text
SchedulingEngine.check-conflict
  receives:  the full Session entity, plus a list of full Session entities
  needs:     startsAt, endsAt, roomName per session

NotificationsAccessor./notifications/send
  receives:  the full Attendee entity + the full Meeting entity
  needs:     toEmail, subject, body

RegistrationsManager inline pricing
  receives:  the full Meeting + the full Registration
  needs:     ticketType, meeting status, days until meeting
```

You can see this for real by inspecting the notifications accessor logs after a
POST to `/registrations`.

---

## 6. Different services need different shapes

The same `Meeting` model is used by many services, but each one needs different
data.

### Public meeting list (gateway)

Needs:

```text
id
title
startsAt
endsAt
status
venueName
venueCity
```

### Meeting details page (gateway)

Needs:

```text
id
title
description
startsAt
endsAt
status
venue
sessions
speakers
feedback summary
```

### Admin meeting list (gateway, admin only)

Needs:

```text
id
title
status
createdAt
registrationsCount
internalNotes
adminOnlyCode
```

### SchedulingEngine.check-capacity

Needs:

```text
venueCapacity
currentRegistrationCount
```

### NotificationsAccessor.send

Needs:

```text
toEmail
subject
body
```

These should probably not all use the same `Meeting` class.

---

## 7. The Gateway is a passthrough

There are no edge models.

The gateway hands back whatever the manager returned.

Public consumers see internal fields, internal IDs, audit timestamps, and full
nested graphs.

---

## 8. No payload versioning

There is no `v1` namespace, no schema-version field, no content negotiation.

If a service needs to change its payload shape, every caller breaks at the
same time.

---

# Main Learning Goal

The main idea is:

```text
In a microservices system, every service-to-service boundary needs its own contract.
EF Core entities are not contracts.
Internal classes are not contracts.
Contracts are explicit, versioned, and owned by the boundary.
```

Before defining a payload that crosses a service boundary, ask:

```text
Who owns this payload — the producer or the consumer?
What version is it?
What is the MINIMUM shape that lets the consumer do its job?
Can the producer and consumer deploy independently?
Does this payload expose data the consumer is not allowed to see?
```
