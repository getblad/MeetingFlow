# MeetingFlow Client-Server Team Exercise

## Team Scope

You are the **Client-Server Team**.

Your projects are:

```text
MeetingFlow.ClientServer/
  MeetingFlow.Api/
  MeetingFlow.Web/
```

You are working on a separated backend and frontend.

The goal is to understand how exposing EF Core entities directly through HTTP can couple the database model, API contract, and React client together.

---

## Purpose of This Exercise

This repository is a teaching project.

The baseline architecture is intentionally imperfect.

The backend intentionally exposes the same EF Core entity models directly through REST endpoints.

The React client intentionally mirrors those backend entities as TypeScript types.

That means the same model shape flows through:

```text
Database
  ↓
EF Core entity
  ↓
REST API JSON response
  ↓
TypeScript type
  ↓
React component
```

This is not recommended production architecture.

It is intentionally written this way so your team can identify the problems and refactor them into better API contracts later.

---

## Project Theme

The project is called **MeetingFlow**.

It is a small meeting/workshop management system.

The domain includes:

- Meetings
- Agenda sessions
- Speakers
- Participants
- Registrations
- Rooms
- Feedback
- Notifications
- Audit logs

The same model is used in many different places.

For example, the `Meeting` model is used for:

- public meeting cards
- meeting details page
- admin meetings table
- dashboard
- registration flow
- optional GraphQL queries
- React TypeScript types

This is the main thing you need to notice.

Different screens need different data, but the baseline project uses one large model everywhere.

---

## Your Projects

```text
MeetingFlow/
  MeetingFlow.ClientServer/
    MeetingFlow.Api/
    MeetingFlow.Web/
```

Backend:

- ASP.NET Core Web API
- EF Core
- SQLite
- REST endpoints
- Optional GraphQL

Frontend:

- React
- TypeScript
- Vite
- React Router
- TanStack Query or simple fetch hooks

---

## Important Rule

Do not start by rewriting the whole application.

The goal is to refactor gradually.

Each task should focus on one endpoint, one screen, or one component.

For example, do not refactor every endpoint at once.

---

# What Is Intentionally Wrong?

The project intentionally contains several architectural problems.

You should identify and discuss them before fixing them.

---

## 1. EF Core entities are exposed directly through the API

The API returns entities like:

```text
Meeting
Speaker
Registration
AuditLogEntry
```

directly from REST endpoints.

This means API consumers depend on the persistence model.

If the database model changes, the API/client can break.

---

## 2. Public and internal data are mixed together

Some entities contain fields like:

```text
InternalNotes
AdminOnlyCode
InternalPaymentReference
TechnicalDetails
RawPayloadJson
```

These fields may be useful internally, but they should not automatically appear in public API responses.

---

## 3. POST endpoints accept entities directly

Some endpoints may accept full entity models from the client.

For example:

```text
POST /api/registrations
```

may accept a full `Registration`.

This is risky because the client may be able to send fields that should only be controlled by the server.

Examples:

```text
payment status
internal payment reference
registration date
server-controlled IDs
```

---

## 4. React components receive huge backend models

A component like `MeetingCard` may receive the full `Meeting` model even though it only needs:

```text
meeting id
title
date
room name
status
```

This creates unnecessary coupling.

The component depends on a large backend shape instead of a small UI-specific shape.

---

## 5. Different screens need different shapes

The same `Meeting` model is used by many screens, but each screen needs different data.

### Meeting card

Needs:

```text
id
title
startsAt
room name
status
```

### Meeting details

Needs:

```text
id
title
description
room
agenda sessions
speakers
feedback
```

### Admin table

Needs:

```text
id
title
status
createdAt
registrations count
internal notes
admin-only code
```

### Dashboard

Needs:

```text
total meetings
total registrations
average rating
upcoming meetings
```

These should probably not all use the exact same model.

---

# Main Learning Goal

The main idea is:

```text
API DTOs are not just extra classes.
They represent contracts between backend and frontend.
```

Before creating an API model, ask:

```text
Who owns this model?
Who consumes this model?
Can this model safely change?
Does this model expose too much?
Does this model match the endpoint or screen?
```

---

# Required Tasks for Client-Server Team

## Task 1: Explore the Client-Server Baseline

Run both projects:

```text
MeetingFlow.Api
MeetingFlow.Web
```

Open:

- public meetings page
- meeting details page
- admin meetings page
- dashboard
- speaker profile page
- audit log page
- registration form

Look at what data is loaded and displayed.

### Deliverable

Write a short note answering:

```text
Where is the same model reused in multiple API responses/screens?
Where does the API expose more data than needed?
Which fields should not be public?
```

---

## Task 2: Identify Entity Leakage Through the API

Find at least three places where EF Core entities are exposed outside the backend.

Examples:

```text
GET /api/meetings returns Meeting directly
GET /api/meetings/{id} returns Meeting directly
GET /api/speakers/{id} returns Speaker directly
POST /api/registrations accepts Registration directly
React MeetingCard receives full Meeting
```

### Deliverable

Create a list like this:

```text
1. File:
   Endpoint/component:
   Problem:
   Why it is risky:

2. File:
   Endpoint/component:
   Problem:
   Why it is risky:

3. File:
   Endpoint/component:
   Problem:
   Why it is risky:
```

---

## Task 3: Refactor Public Meetings API

Start with the smallest useful refactor.

Refactor the public meetings endpoint so it no longer returns the full `Meeting` entity.

Create a smaller public meeting list response shape.

It should contain only:

```text
id
title
startsAt
endsAt
status
roomName
roomCity
```

### Backend goal

Instead of returning full `Meeting` entities from the public meetings endpoint, return a smaller public meeting list shape.

### Frontend goal

Update the React meetings page so `MeetingCard` receives only the data it needs.

### Deliverable

Show before/after:

```text
Before:
GET /api/meetings returned full Meeting.
MeetingCard received full Meeting.

After:
GET /api/meetings returns a smaller public meeting list shape.
MeetingCard receives only the fields it needs.
```

---

## Task 4: Refactor Meeting Details API

Create a separate shape for the meeting details page.

The meeting details page needs more data than the meeting card, but it still should not expose everything.

It may include:

```text
id
title
description
startsAt
endsAt
status
room
agenda sessions
speakers
feedback summary
```

It should not expose:

```text
internal notes
admin-only code
raw technical data
payment references
```

### Deliverable

Explain why the meeting list response and meeting details response should be separate.

---

## Task 5: Refactor Registration Create Endpoint

The registration form should not send a full `Registration` entity.

Create a dedicated input shape for creating a registration.

It should allow the client to send only:

```text
meetingId
participant full name
participant email
participant phone
ticket type
```

The server should control:

```text
registration id
registeredAt
payment status
internal payment reference
```

### Deliverable

Explain what fields the client should control and what fields the server should control.

---

## Task 6: Refactor Admin Meetings API

The admin meetings table needs different data from the public meeting list.

Create a dedicated admin meeting table response shape.

It may include:

```text
id
title
status
createdAt
roomName
registrationsCount
internalNotes
adminOnlyCode
```

### Deliverable

Explain why admin API data should be separated from public API data.

---

## Task 7: Refactor Speaker Profile API

The public speaker profile should not expose every field from the `Speaker` entity.

Public speaker profile may include:

```text
id
fullName
bio
company
agenda sessions
```

It should probably not expose:

```text
email
phone
internal notes
```

### Deliverable

Explain which speaker fields are public and which are internal.

---

## Task 8: Refactor Audit Log API

Audit logs may contain technical or sensitive details.

Decide what should be returned to the UI.

The UI may show:

```text
entity type
entity id
action
actor name
createdAt
```

But maybe hide:

```text
technical details
raw exception data
internal payloads
```

### Deliverable

Explain whether `TechnicalDetails` should be returned by the API and to whom.

---

# Optional Advanced Tasks

## Task 9: Add OpenAPI-Generated Types

Generate TypeScript types from the backend API contract.

Goal:

```text
The frontend should not manually duplicate backend entity models.
```

Compare:

```text
Manual TypeScript entity mirror
```

versus:

```text
Generated API types
```

### Deliverable

Explain the pros and cons of generated API contracts.

---

## Task 10: Compare REST DTOs and GraphQL Field Selection

If GraphQL exists in the project, compare two approaches.

### REST with endpoint-specific API models

```text
GET /api/meetings
returns a specific public meeting list shape
```

### GraphQL

```graphql
query {
  meetings {
    id
    title
    startsAt
    room {
      name
    }
  }
}
```

### Deliverable

Answer:

```text
Does GraphQL remove the need for DTOs completely?
What problem does GraphQL solve?
What problem remains if GraphQL exposes EF Core entities directly?
```

---

## Task 11: Create Integration Event Contracts

Create event contracts for async communication.

Examples:

```text
MeetingCreatedV1
ParticipantRegisteredV1
FeedbackSubmittedV1
```

These should describe business facts that happened.

They should not contain full EF Core entity graphs.

### Example

```text
ParticipantRegisteredV1
- meetingId
- participantId
- participantEmail
- registeredAt
```

### Deliverable

Explain the difference between:

```text
API response model
```

and:

```text
integration event contract
```

---

# Suggested Refactoring Order

Use this order:

```text
1. Public meetings endpoint + MeetingCard
2. Meeting details endpoint
3. Registration create endpoint
4. Admin meetings endpoint
5. Speaker profile endpoint
6. Audit log endpoint
7. OpenAPI or GraphQL comparison
8. Integration events
```

This order starts with the easiest and most visible problem.

---

# Questions to Ask During the Exercise

```text
1. Does this screen need the full entity?
2. Is this model crossing a boundary?
3. Is this data public, admin-only, or internal?
4. Who is allowed to set this field?
5. Can this shape change without breaking other screens?
6. Is this model designed for the database or for the consumer?
7. Should this be a response model, input model, GraphQL type, or event contract?
```

---

# Expected Final Outcome for Client-Server Team

By the end of the exercise, you should understand:

- why exposing EF Core entities through APIs is risky
- why frontend TypeScript types should not blindly mirror database entities
- why input models and output models should usually be separate
- why public and admin data should be separated
- why React components should not depend on huge backend entity models
- how REST API models, GraphQL schema types, generated OpenAPI types, and integration events differ

You do not need to refactor the entire client-server app perfectly.

The goal is to understand the boundaries and make intentional architectural decisions.
