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
