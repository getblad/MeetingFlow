# MeetingFlow — DTO Architecture Teaching Baseline

## Purpose

This repository intentionally uses **one model everywhere** — the same EF Core entity flows from the database through application logic, API endpoints, and UI pages. Students will study this codebase to understand **why exposing database/domain models directly through application boundaries is problematic**, and then refactor the projects into proper DTO/contract architectures.

> **This repository intentionally exposes EF Core/domain entities directly through UI and API boundaries. This is not recommended production architecture. It is a teaching baseline.**

---

## Repository Structure

```
MeetingFlow/
├── README.md                          # This file
├── MeetingFlow.sln                      # Solution file (both .NET projects)
│
├── MeetingFlow.Monolith/                # Project 1: Server-rendered monolith
│   ├── Models/                        # EF Core entity models
│   ├── Data/                          # DbContext + seed data
│   ├── Pages/                         # Razor Pages (UI)
│   └── Program.cs
│
└── MeetingFlow.ClientServer/            # Project 2: Client-server architecture
    ├── MeetingFlow.Api/                 # ASP.NET Core Web API
    │   ├── Models/                    # EF Core entity models (same as monolith)
    │   ├── Data/                      # DbContext + seed data
    │   ├── Endpoints/                 # Minimal API endpoints + GraphQL
    │   └── Program.cs
    │
    └── MeetingFlow.Web/                 # React + TypeScript + Vite frontend
        └── src/
            ├── types/models.ts        # TypeScript types mirroring backend entities
            ├── api/                   # HTTP client functions
            ├── components/            # React components
            └── pages/                 # React pages
```

### MeetingFlow.Monolith

A traditional ASP.NET Core monolith using Razor Pages. EF Core entity models are used directly from database queries all the way to `.cshtml` views. No ViewModels, no DTOs, no mapping layers.

### MeetingFlow.ClientServer

A client-server architecture with:

- **MeetingFlow.Api** — ASP.NET Core Web API exposing EF Core entities directly through REST endpoints and optional GraphQL (Hot Chocolate).
- **MeetingFlow.Web** — React + TypeScript + Vite SPA that mirrors backend entities as TypeScript types.

---

## How to Run

### MeetingFlow.Monolith

```bash
cd MeetingFlow.Monolith
dotnet restore
dotnet run
```

Open [http://localhost:5000](http://localhost:5000) (or the URL shown in console output).

The SQLite database (`meetingflow_monolith.db`) is created and seeded automatically on first run.

### MeetingFlow.ClientServer

**Backend (API):**

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Api
dotnet restore
dotnet run
```

The API runs on [http://localhost:5062](http://localhost:5062) by default.
GraphQL playground is available at [http://localhost:5062/graphql](http://localhost:5062/graphql).
The SQLite database (`meetingflow_api.db`) is created and seeded automatically on first run.

**Frontend (React):**

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173).
The Vite dev server proxies `/api` and `/graphql` requests to the API.

---

## Domain Model

The domain covers meeting/conference management with these entities:

| Entity            | Purpose                                                 |
| ----------------- | ------------------------------------------------------- |
| **Meeting**       | A conference or meetup with title, dates, status, venue |
| **Session**       | A talk or workshop within an meeting                    |
| **Speaker**       | A person giving a session                               |
| **Attendee**      | A person attending meetings                             |
| **Registration**  | Links an attendee to an meeting with ticket info        |
| **Venue**         | Physical location hosting meetings                      |
| **Feedback**      | Attendee ratings and comments on meetings               |
| **Notification**  | Messages sent to attendees (email, SMS, push)           |
| **AuditLogEntry** | System audit trail for entity changes                   |

---

## What Is Intentionally Wrong

This codebase demonstrates these architectural smells **on purpose**:

1. **Database/persistence models are exposed directly** — EF Core entities are returned from API endpoints and used in Razor Pages without any intermediate layer.
2. **Public pages can access internal fields** — Fields like `InternalNotes`, `AdminOnlyCode`, and `InternalPaymentReference` are available in public-facing views and API responses.
3. **Admin-only fields exist on the same model as public fields** — No separation between what admin vs. public users should see.
4. **POST endpoints accept too much data** — The `Registration` entity is accepted directly, allowing clients to set fields they shouldn't control.
5. **React components depend on huge backend entity shapes** — Components receive full entities even when they only use a few fields.
6. **Different screens need different data shapes, but all use the same model** — Meeting cards, meeting details, admin tables, dashboard items, and calendar items all use the same `Meeting` entity.
7. **API responses over-fetch nested data** — Navigation properties are eagerly loaded and returned in full.
8. **Changes to EF entities would break API/client contracts** — There's no buffer between the database schema and the API contract.
9. **GraphQL exposes persistence model directly** — The GraphQL schema mirrors EF Core entities, allowing clients to query any field including internal data.
10. **No clear boundary between domain, persistence, API, and UI models** — One class does everything.

---

## Student Refactoring Exercises

### Exercise 1: REST Response DTOs

Introduce endpoint-specific response types:

- `MeetingCardResponse` — only `id`, `title`, `startsAt`, `status`, `venueName`, `venueCity`
- `MeetingDetailsResponse` — meeting info + sessions + speakers + feedback count
- `AdminMeetingRowResponse` — includes `internalNotes`, `adminOnlyCode`, registration count
- `SpeakerProfileResponse` — excludes `email`, `phone`, `internalNotes`

### Exercise 2: Request Models

Create separate request models for POST/PUT endpoints:

- `CreateRegistrationRequest` — only `meetingId`, `attendeeId`, `ticketType`
- `UpdateMeetingRequest` — only the fields an admin should be able to modify

### Exercise 3: Razor Page ViewModels

Create page-specific ViewModels for MeetingFlow.Monolith:

- `MeetingDetailsViewModel`
- `AdminMeetingsViewModel`
- `DashboardViewModel`

### Exercise 4: Shared Kernel / Value Objects

Introduce a small SharedKernel with strongly-typed IDs and value objects:

- `MeetingId`, `SpeakerId`, `AttendeeId`
- `Money` (for ticket pricing)
- `DateRange` (for meeting/session time spans)

### Exercise 5: OpenAPI + Generated Client

Add OpenAPI generation for MeetingFlow.Api and generate TypeScript client/types for MeetingFlow.Web using tools like `openapi-typescript` or NSwag.

### Exercise 6: Component-Specific Props

Refactor React components to depend on smaller, component-specific props instead of full backend entities:

- `MeetingCard` should accept `{ id, title, startsAt, status, venueName, venueCity }`
- `SessionList` should accept `{ id, title, startsAt, endsAt, roomName, speakerName }[]`

### Exercise 7: GraphQL Schema Types

If GraphQL is implemented, refactor GraphQL schema types away from EF Core entities. Create dedicated GraphQL types that control which fields are queryable.

### Exercise 8: Integration Meeting Contracts

Create integration meeting contracts for an meeting-driven architecture:

- `MeetingCreatedV1`
- `AttendeeRegisteredV1`
- `FeedbackSubmittedV1`

### Exercise 9: Compare Approaches

Compare and contrast these DTO/contract strategies:

- Local DTOs per endpoint/page
- Shared contracts library
- OpenAPI-generated contracts
- GraphQL schema as contract
- Meeting contracts for async messaging
- Shared kernel with value objects
- No DTOs (this baseline)

### Exercise 10: Security Audit

Identify fields that should **never** be exposed publicly and create a plan to remove them from public responses:

- `InternalNotes` (on Meeting, Session, Speaker, Attendee)
- `AdminOnlyCode` (on Meeting)
- `InternalPaymentReference` (on Registration)
- `TechnicalDetails` (on AuditLogEntry)
- `RawPayloadJson` (on Notification)
- `Email`, `Phone` (on Speaker, in public-facing contexts)
- `InternalContactName`, `InternalContactPhone` (on Venue)
- `ModerationNotes` (on Feedback)

---

## Tech Stack

| Layer    | Technology                                                           |
| -------- | -------------------------------------------------------------------- |
| Monolith | ASP.NET Core, Razor Pages, EF Core, SQLite                           |
| API      | ASP.NET Core, Minimal APIs, EF Core, SQLite, Hot Chocolate (GraphQL) |
| Frontend | React, TypeScript, Vite, React Router                                |
