# MeetingFlow microservices tests

This directory contains examples for the components and integration tests
lecture. The examples are added incrementally so each test boundary remains
visible.

Planned test-suite boundaries:

- `MeetingFlow.SchedulingEngine.ComponentTests` — starts the complete
  SchedulingEngine HTTP application in the test process with
  `WebApplicationFactory`.
- `MeetingFlow.IntegrationTests` — a specific integration between real components.

## SchedulingEngine component tests

Install/restore the test dependencies and run the project from the repository
root:

```bash
dotnet restore MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
dotnet test MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
```

`WebApplicationFactory<Program>` boots the real Minimal API with an in-memory
test server. Requests still pass through ASP.NET Core routing, JSON
serialization, model binding, validation endpoints and response serialization,
but no TCP port, Docker container or external service is required.
