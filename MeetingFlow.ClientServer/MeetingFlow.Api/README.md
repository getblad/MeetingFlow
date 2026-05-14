# MeetingFlow.Api

ASP.NET Core Web API using Minimal APIs, EF Core, and Hot Chocolate (GraphQL).

## Architecture

This project intentionally returns EF Core entity models directly from API endpoints. There are no DTOs, response models, or mapping layers.

- **Models/** — EF Core entity classes (returned directly from endpoints)
- **Data/** — `MeetingFlowDbContext` and `SeedData`
- **Endpoints/** — Minimal API endpoint mappings + GraphQL setup

## REST Endpoints

| Method | Path                  | Description                                |
| ------ | --------------------- | ------------------------------------------ |
| GET    | `/api/meetings`       | List all meetings with venues and sessions |
| GET    | `/api/meetings/{id}`  | Meeting details with full entity graph     |
| GET    | `/api/admin/meetings` | Admin view with internal fields            |
| PUT    | `/api/meetings/{id}`  | Update meeting (accepts full entity)       |
| GET    | `/api/speakers/{id}`  | Speaker profile with sessions              |
| POST   | `/api/registrations`  | Create registration (accepts full entity)  |
| GET    | `/api/dashboard`      | Dashboard analytics                        |
| GET    | `/api/audit-log`      | Audit log entries                          |

## GraphQL

Available at `/graphql` with the Banana Cake Pop IDE.

## Running

```bash
dotnet restore
dotnet run
```

The SQLite database is created and seeded automatically on startup.

## What's Intentionally Wrong

- All endpoints return full EF Core entities including internal/sensitive fields
- POST/PUT endpoints accept the full entity directly
- GraphQL exposes the persistence model directly
- No authentication or authorization
- No response shaping or field filtering
