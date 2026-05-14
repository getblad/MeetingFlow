# MeetingFlow.Monolith

ASP.NET Core server-rendered monolith using Razor Pages.

## Architecture

This project intentionally uses EF Core entity models directly from database queries to Razor Page views. There are no ViewModels, DTOs, or mapping layers.

- **Models/** — EF Core entity classes (used everywhere)
- **Data/** — `MeetingFlowDbContext` and `SeedData`
- **Pages/** — Razor Pages (server-rendered UI)
- **wwwroot/** — Static files (CSS)

## Running

```bash
dotnet restore
dotnet run
```

The SQLite database is created and seeded automatically on startup.

## What's Intentionally Wrong

- Razor Pages bind directly to EF Core entities
- Internal fields (InternalNotes, AdminOnlyCode) are available in all views
- The Registration create page binds directly to the entity model
- No separation between public and admin data shapes
