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
├── MeetingFlow.ClientServer/            # Project 2: Client-server architecture
│   ├── MeetingFlow.Api/                 # ASP.NET Core Web API
│   │   ├── Models/                    # EF Core entity models (same as monolith)
│   │   ├── Data/                      # DbContext + seed data
│   │   ├── Endpoints/                 # Minimal API endpoints + GraphQL
│   │   └── Program.cs
│   │
│   └── MeetingFlow.Web/                 # React + TypeScript + Vite frontend
│       └── src/
│           ├── types/models.ts        # TypeScript types mirroring backend entities
│           ├── api/                   # HTTP client functions
│           ├── components/            # React components
│           └── pages/                 # React pages
│
└── MeetingFlow.Microservices/           # Project 3: IDesign-style microservices
    ├── docker-compose.yml             # Postgres + 6 services
    ├── infra/postgres/init.sql        # Four schemas in one Postgres container
    ├── src/
    │   ├── Gateway/                   # Public HTTP edge (Client)
    │   ├── Managers/                  # MeetingsManager, RegistrationsManager
    │   ├── Engines/                   # SchedulingEngine (pure logic)
    │   └── Accessors/                 # DataAccessor, NotificationsAccessor
    └── tests/                         # xUnit integration tests (fail by design)
```

### MeetingFlow.Monolith

A traditional ASP.NET Core monolith using Razor Pages. EF Core entity models are used directly from database queries all the way to `.cshtml` views. No ViewModels, no DTOs, no mapping layers.

### MeetingFlow.ClientServer

A client-server architecture with:

- **MeetingFlow.Api** — ASP.NET Core Web API exposing EF Core entities directly through REST endpoints and optional GraphQL (Hot Chocolate).
- **MeetingFlow.Web** — React + TypeScript + Vite SPA that mirrors backend entities as TypeScript types.

### MeetingFlow.Microservices

An IDesign-style microservices system (one Gateway, two Managers, one Engine, two Resource Accessors) running on Docker Compose with Postgres. Each service redeclares its own near-duplicate copy of every entity, and the same shape flows from the database through every service to the public gateway response. No shared contracts library, no edge DTOs, no payload versioning.

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

### MeetingFlow.Microservices

```bash
cd MeetingFlow.Microservices
docker compose up --build
```

The public gateway is available at [http://localhost:8080](http://localhost:8080). Postgres comes up first (with a healthcheck) and the six services start in dependency order. The schemas (`meetings`, `registrations`, `feedback`, `notifications`) are created via `infra/postgres/init.sql` and seeded automatically on first start.

To run the integration tests against the live stack (from this directory):

```bash
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests
```

All tests are expected to fail against the baseline — that's the teaching signal.

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

## Tech Stack

| Layer         | Technology                                                           |
| ------------- | -------------------------------------------------------------------- |
| Monolith      | ASP.NET Core, Razor Pages, EF Core, SQLite                           |
| API           | ASP.NET Core, Minimal APIs, EF Core, SQLite, Hot Chocolate (GraphQL) |
| Frontend      | React, TypeScript, Vite, React Router                                |
| Microservices | ASP.NET Core Minimal APIs, EF Core, Postgres, Docker Compose         |
