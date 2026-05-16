# MeetingFlow Monolith Team Exercise

## Team Scope

You are the **Monolith Team**.

Your project is:

```text
MeetingFlow.Monolith
```

You are working on a traditional server-rendered monolith.

The goal is to understand how using the same EF Core entity models directly in Razor Pages/MVC views can create coupling and accidental data exposure.

---

## Purpose of This Exercise

This repository is a teaching project.

The baseline architecture is intentionally imperfect.

The monolith intentionally uses the same EF Core entity models all the way from the database to the UI.

That means the same classes are used for:

```text
Database
  ↓
Backend logic
  ↓
Razor Page / MVC View
  ↓
Rendered HTML
```

This is not recommended production architecture.

It is intentionally written this way so your team can identify the problems and refactor them into better page-specific models later.

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

This is the main thing you need to notice.

Different pages need different data, but the baseline project uses one large model everywhere.

---

## Your Project

```text
MeetingFlow/
  MeetingFlow.Monolith/
```

The monolith uses:

- ASP.NET Core
- Razor Pages or MVC
- EF Core
- SQLite
- Server-rendered UI

No React is needed for your team.

---

## Important Rule

Do not start by rewriting the whole application.

The goal is to refactor gradually.

Each task should focus on one page, one form, or one boundary.

For example, do not refactor every page at once.

---

# What Is Intentionally Wrong?

The project intentionally contains several architectural problems.

You should identify and discuss them before fixing them.

---

## 1. EF Core entities are passed directly to pages

Pages receive entities like:

```text
Meeting
Speaker
Registration
AuditLogEntry
```

directly from EF Core queries.

This means the UI depends on the persistence model.

If the database model changes, the page can break.

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

These fields may be useful internally, but they should not automatically be available to public pages.

---

## 3. Forms may bind directly to entities

Some forms may bind directly to full entity models.

For example, a meeting registration form may bind directly to `Registration`.

This is risky because the user might be able to submit fields that should only be controlled by the server.

Examples:

```text
payment status
internal payment reference
registration date
server-controlled IDs
```

---

## 4. Different pages need different shapes

The same `Meeting` model is used by many pages, but each page needs different data.

### Public meeting card

Needs:

```text
id
title
startsAt
room name
status
```

### Meeting details page

Needs:

```text
id
title
description
room
agenda sessions
speakers
feedback summary
```

### Admin meeting table

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
Page models / view models are not just extra classes.
They represent boundaries between persistence and UI.
```

Before creating a page-specific model, ask:

```text
Who owns this model?
Who consumes this model?
Can this model safely change?
Does this model expose too much?
Does this model match the page?
```

---

# Required Tasks for Monolith Team

## Task 1: Explore the Monolith Baseline

Run `MeetingFlow.Monolith`.

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
Where is the same model reused in multiple pages?
Where does the app expose more data than needed?
Which fields should not be public?
```

---

## Task 2: Identify Entity Leakage in Pages

Find at least three places where EF Core entities are passed directly to the UI.

Examples:

```text
Razor page receives Meeting directly
Razor page receives Speaker directly
registration form binds directly to Registration
audit log page displays AuditLogEntry directly
```

### Deliverable

Create a list like this:

```text
1. File:
   Problem:
   Why it is risky:

2. File:
   Problem:
   Why it is risky:

3. File:
   Problem:
   Why it is risky:
```

---

## Task 3: Refactor Public Meetings Page

Start with the smallest useful refactor.

Refactor the public meetings page so it no longer depends on the full `Meeting` entity.

Create a smaller page-specific shape for the public meeting list.

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

### Goal

The public meetings page should receive only the data it needs.

It should not have access to:

```text
internal notes
admin-only code
registrations
technical data
```

### Deliverable

Show before/after:

```text
Before:
The page received full Meeting entities.

After:
The page receives a smaller public meeting list model.
```

---

## Task 4: Refactor Meeting Details Page

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

Explain why the meeting list shape and meeting details shape should be separate.

---

## Task 5: Refactor Registration Form

The registration form should not bind directly to a full `Registration` entity.

Create a dedicated input/page model for creating a registration.

It should allow the user to submit only:

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

Explain what fields the user should control and what fields the server should control.

---

## Task 6: Refactor Admin Meetings Table

The admin meetings table needs different data from the public meeting list.

Create a dedicated admin meeting table model.

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

Explain why admin data should be separated from public data.

---

## Task 7: Refactor Speaker Profile

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

## Task 8: Refactor Audit Log Page

Audit logs may contain technical or sensitive details.

Decide what should be shown in the UI.

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

Explain whether `TechnicalDetails` should be visible and to whom.

---

# Optional Advanced Tasks

## Task 9: Create a Small Shared Kernel

Introduce stable value objects or IDs.

Examples:

```text
MeetingId
SpeakerId
ParticipantId
DateRange
```

### Deliverable

Explain why these are safer to share than a giant `Meeting` model.

---

## Task 10: Compare Monolith Page Models with API DTOs

Answer:

```text
What is the monolith equivalent of an API response DTO?
Why do Razor pages need page-specific models?
When is it okay for a Razor page to use an entity directly?
```

---

# Suggested Refactoring Order

Use this order:

```text
1. Public meetings page
2. Meeting details page
3. Registration form
4. Admin meetings table
5. Speaker profile
6. Audit log page
```

This order starts with the easiest and most visible problem.

---

# Questions to Ask During the Exercise

```text
1. Does this page need the full entity?
2. Is this model crossing a boundary?
3. Is this data public, admin-only, or internal?
4. Who is allowed to set this field?
5. Can this shape change without breaking other pages?
6. Is this model designed for the database or for the UI?
7. Should this be a page model, input model, or entity?
```

---

# Expected Final Outcome for Monolith Team

By the end of the exercise, you should understand:

- why passing EF Core entities directly to pages can be risky
- why public and admin data should be separated
- why forms should not bind directly to full entities
- why different pages need different shapes
- what page-specific models/view models are for
- where DTO-like models make sense in a monolith

You do not need to refactor the entire monolith perfectly.

The goal is to understand the boundaries and make intentional architectural decisions.
