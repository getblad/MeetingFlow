# Practical Part of the Lecture: Component, Integration, and System Tests in MeetingFlow

This is a speaker script for the practical demo after the theory slides.
**What I show** tells me which files to open. **What I say** can be used almost
word for word.

## Before the Demo

### What I show

- the solution opened from the folder that contains `MeetingFlow.slnx`;
- `MeetingFlow.Microservices/tests` in VS Code;
- the Testing panel with the discovered xUnit tests;
- Docker Desktop running.

### What I say

> I will show five examples. In each example, I will first show the production
> component, then the test dependencies and fixture, and finally the test and
> how to run it. Docker is not needed for every example. We will see which parts
> run inside the test process and which parts run in containers.

---

## 1. SchedulingEngine with `WebApplicationFactory`

### What I show

1. `src/Engines/SchedulingEngine/Program.cs`;
2. `tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj`;
3. `tests/MeetingFlow.SchedulingEngine.ComponentTests/SchedulingEngineComponentTests.cs`.

In the `.csproj`, I show `Microsoft.AspNetCore.Mvc.Testing` and the project
references to `SchedulingEngine` and its contracts.

> `Microsoft.AspNetCore.Mvc.Testing` is a NuGet package. It provides
> `WebApplicationFactory`. The production project reference gives the test
> access to the `Program` entry point. The contracts reference lets us use the
> same models as a real client.

### What the component does

`SchedulingEngine` provides two HTTP operations:

- `/scheduling/check-conflict` checks whether sessions overlap in the same room;
- `/scheduling/check-capacity` calculates the number of available places.

It has no database, message broker, or downstream HTTP services.

### What I say about the boundary

> The whole SchedulingEngine is the system under test. I do not call a private
> algorithm function. The test enters through a real HTTP endpoint, so routing,
> JSON serialization, model binding, validation, and response serialization all
> run. This is why it is not a unit test, even though the service is small.

> `WebApplicationFactory<Program>` starts the application inside the testhost
> process. We do not need a real TCP port or a Docker container. `CreateClient()`
> creates a client connected to an in-memory test server.

> The request represents a production request from MeetingsManager or
> RegistrationsManager. SchedulingEngine does not call anything else, so there
> are no external dependencies to replace.

### Why we need `public partial class Program`

I show the end of production `Program.cs`:

```csharp
public partial class Program { }
```

### What I say

> With minimal APIs, the compiler generates the entry point. This empty partial
> declaration makes `Program` visible to the test project, so
> `WebApplicationFactory<Program>` can find and start the application. It has no
> test logic and does not change production behavior.

### I show a `Theory`

```csharp
[Theory]
[InlineData(..., true)]
[InlineData(..., false)]
```

### What I say

> These rows check the same rule with different input data. A `Theory` is a
> better fit than several almost identical `Fact` tests.

I explain the cases:

- the same room and overlapping time means a conflict;
- `11:00–12:00` after `10:00–11:00` is not a conflict;
- overlapping time in another room is not a conflict;
- an interval before the existing session is not a conflict;
- room name comparison is case-insensitive.

> A `Theory` works when Arrange, Act, and Assert stay the same and only the data
> and expected result change. Different behavior with different failure reasons
> is easier to read in separate tests.

### I show separate `Fact` tests

- an invalid time range returns a validation problem;
- a valid capacity request returns the number of available places;
- a negative capacity returns `400`.

### What I say

> An invalid time range is different HTTP behavior, so it has a separate
> `Fact`. We check not only the Boolean result, but also the error contract:
> status `400` and the `candidate` key in the validation problem.

### I run the tests

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
```

I can also click Play next to a test in VS Code.

### Alternative

> We could move the pure algorithm into a separate class and test it with unit
> tests. They would be faster, but they would not check routing, binding, or the
> HTTP contract. For a complex algorithm, we can keep many edge cases in unit
> tests and only a few representative cases in the component suite.

---

## 2. DataAccessor with a Real PostgreSQL Database

### What I show

1. `tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj`;
2. `DataAccessorFixture.cs`;
3. `DataAccessorComponentTests.cs`.

### Installed libraries

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" ... />
<PackageReference Include="Testcontainers.PostgreSql" ... />
<PackageReference Include="Respawn" ... />
```

### What I say

> An in-memory collection is not enough for a useful DataAccessor test. The
> component is responsible for EF Core queries, relations, PostgreSQL mapping,
> and persistence. We start DataAccessor with `WebApplicationFactory` and start
> PostgreSQL in a disposable Testcontainer.

> Testcontainers uses the real `postgres:16` image on a dynamic host port and
> gives the connection string to the test. The test does not care whether local
> port `5432` is busy, and it cannot connect to a developer database by mistake.

### First, I explain the fixture

```csharp
private readonly PostgreSqlContainer _postgres = ...;
private WebApplicationFactory<Program>? _application;
private HttpClient? _client;
private Respawner? _respawner;
```

### What I say

> A fixture stores the shared test environment and manages its lifecycle. This
> fixture starts PostgreSQL and DataAccessor, creates an HTTP client, configures
> Respawn, and releases the resources at the end.

> The setup is not copied into every test. Tests receive a ready `HttpClient`
> and methods for preparing and cleaning data. A fixture is not a mock or a type
> of test. It is a way to organize reusable setup, state, and cleanup.

### I explain the fixture lifecycle

```csharp
public sealed class DataAccessorComponentTests(DataAccessorFixture fixture)
    : IClassFixture<DataAccessorFixture>, IAsyncLifetime
```

```csharp
public Task InitializeAsync() => fixture.ResetDatabaseAsync();
```

### What I say

> `IClassFixture<DataAccessorFixture>` creates one fixture for this test class.
> PostgreSQL starts once for the class, not once for every `Fact`. xUnit still
> creates a new test class instance for every test case.

> One container per class is simple and safe while the suite is small. If many
> classes make startup slow, we can share a container at collection or assembly
> level. We then need separate databases or schemas and a clear strategy for
> isolation and parallel execution.

### I show the application configuration

```csharp
builder.UseSetting("POSTGRES_CONN", _postgres.GetConnectionString());
```

### What I say

> We do not replace production dependency injection. We only replace the
> connection string. DataAccessor follows its normal startup path and uses the
> real `MeetingFlowDbContext` and Npgsql provider.

### I explain seed data and Respawn

> Production startup creates the schema and inserts seed data, but the tests do
> not depend on those records. After application startup, the fixture configures
> Respawn. Before every test, Respawn cleans the application schemas.

> Respawn does not recreate the container or run migrations again. It clears
> tables in the correct dependency order. The schema remains, but every test
> starts with a known data state. Changing production seed must not change the
> test result.

### How the test creates its own data

```csharp
public async Task SeedAsync<TEntity>(params TEntity[] entities)
    where TEntity : class
{
    using var scope = _application!.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MeetingFlowDbContext>();

    db.Set<TEntity>().AddRange(entities);
    await db.SaveChangesAsync();
}
```

> Direct EF setup is acceptable in a component test of DataAccessor because the
> database and EF model are inside this component boundary. The generic helper
> works with any EF entity without `object[]` or runtime type checks.

> Different entity types are passed in separate calls: `SeedAsync(venue)`, then
> `SeedAsync(meeting)`, and then `SeedAsync(attendee)`. The order follows the
> foreign-key dependencies.

> Direct SQL or EF setup is not always right for a system test. At system level,
> it couples the test to another service's schema and bypasses the business API.
> Another option here is to create prerequisites through the Accessor HTTP API.
> That reduces EF coupling but makes Arrange longer and may test extra endpoints.

### Test 1: read the complete meeting graph

I show `GetMeeting_WhenMeetingExists_ReturnsGraphLoadedFromPostgreSql`.

> The test creates a venue, meeting, session, speaker, registration, attendee,
> and feedback. It sends an HTTP GET request and checks that the real EF query
> loads the relations and that DataAccessor returns the correct DTO.

> We also set `InternalNotes` and `AdminOnlyCode` and inspect the raw JSON. These
> internal EF fields must not cross the HTTP contract boundary.

### Test 2: write and read a registration

I show `CreateRegistration_WhenReferencesExist_PersistsItInPostgreSql`.

> The test first creates only the required foreign-key data. It creates the
> registration through HTTP. A separate GET reads the registration list. This
> proves that the row was committed to PostgreSQL, not only created in memory.

### Test 3: a missing record

> We check `404` for a random ID. No Arrange is needed because Respawn removes
> other data and a new GUID makes the scenario independent.

### I run the tests

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj
```

While the tests run, I open Docker Desktop and show the new containers.

### I explain the second Testcontainers container

> Ryuk appears next to PostgreSQL. It is the Testcontainers resource reaper. It
> tracks Docker resources created by the test process and helps remove them even
> if the test process stops unexpectedly.

> It is not a second database or another MeetingFlow service. PostgreSQL is the
> only product infrastructure in this test. Ryuk only manages test resources.

After the suite, I show that PostgreSQL and Ryuk are no longer running.

---

## 3. RegistrationsManager with HTTP Stubs and a Spy

### What I show

1. the production flow in `src/Managers/RegistrationsManager/Program.cs`;
2. the test project `.csproj`;
3. `RegistrationsManagerFixture.cs`;
4. `RegistrationsManagerComponentTests.cs`;
5. `SpyEventPublisher.cs`.

In the `.csproj`, I show `Microsoft.AspNetCore.Mvc.Testing`, `WireMock.Net`, and
the project references to the Manager and its contracts.

> `WebApplicationFactory` starts the Manager. `WireMock.Net` gives us two local
> HTTP servers for DataAccessor and SchedulingEngine responses. We do not need a
> mock framework for the publisher. The small `SpyEventPublisher` is easier to
> show and explain directly in the code.

### First, I explain the Manager's responsibility

> RegistrationsManager gets the meeting and attendee, checks for an existing
> registration, asks SchedulingEngine about capacity, calculates the price,
> saves the registration, and publishes an integration event.

> Its responsibility is orchestration. We keep the real Manager and HTTP
> pipeline, but control its external dependencies.

### What is real and what is replaced

```text
Real:
  RegistrationsManager;
  endpoint, validation, and orchestration;
  typed HttpClient and HTTP serialization to the stub servers;
  pricing logic.

Replaced:
  DataAccessor          → WireMock HTTP stub;
  SchedulingEngine      → WireMock HTTP stub;
  RabbitMQ publisher    → in-memory spy;
  system time           → StubTimeProvider.
```

### Why WireMock instead of a mock `DataAccessorClient`

> We could mock a C# client interface. That would be faster and closer to a unit
> test. WireMock starts a real local HTTP server. The production typed client
> builds the URL, serializes the body, and reads an HTTP response. This checks
> more of the HTTP interaction without starting DataAccessor and PostgreSQL.

> A stub returns prepared answers. A spy records output interactions for later
> assertions. People often call all test doubles mocks, but this distinction is
> useful: a stub gives input, while a spy records output.

### I show the fixture configuration

```csharp
builder.UseSetting("DATA_ACCESSOR_URL", DataAccessorStub.Url!);
builder.UseSetting("SCHEDULING_ENGINE_URL", SchedulingEngineStub.Url!);
```

and the dependency injection replacements:

```csharp
services.RemoveAll<IEventPublisher>();
services.AddSingleton<IEventPublisher>(EventPublisher);
services.RemoveAll<TimeProvider>();
services.AddSingleton<TimeProvider>(fixedTime);
```

### What I say

> A seam is a place where we can replace a production dependency without
> changing business code. RabbitMQ is behind `IEventPublisher`, and time comes
> from `TimeProvider`. We fix time because the price depends on the date.
> Otherwise, the same test could return a different price next week.

### Why `Reset()` is called in the test class constructor

> The fixture is created once per class. WireMock logs and published events stay
> in it between tests. xUnit creates a new test class instance for each `Fact`,
> so `_fixture.Reset()` in the constructor clears the stubs, request logs, and
> spy before each scenario.

> The method name `Reset` has no special meaning. We only need to isolate mutable
> fixture state. Alternatives are a fixture per test,
> `IAsyncLifetime.InitializeAsync`, separate stub servers, or immutable setup.
> The constructor is convenient here because reset is short and synchronous.

### Successful registration test

I show these steps:

1. meeting and attendee stubs;
2. an empty list of registrations;
3. the SchedulingEngine response;
4. the save response;
5. the POST request to the Manager;
6. assertions on the response, outgoing requests, and event.

### What I say

> We check more than the final `201`. The Manager must send the correct data
> downstream: capacity `800`, registration count `0`, and ticket type `General`.
> The spy then confirms the routing key and versioned event content.

> These are interaction assertions. We should not overuse them because they can
> couple tests to implementation details. Here, external interactions are part
> of the Manager's visible responsibility.

### Duplicate registration test

> The stub returns an existing registration for the attendee. SchedulingEngine
> is not configured. We expect `409`, no SchedulingEngine requests, and no
> event. This also proves that the flow stops early after a business rejection.

### Full meeting test

> Capacity equals the current registration count, so SchedulingEngine returns
> `HasCapacity=false`. We check that the Manager does not save a registration or
> publish an event.

### I run the tests

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.RegistrationsManager.ComponentTests/MeetingFlow.RegistrationsManager.ComponentTests.csproj
```

### Alternative

> We could start real DataAccessor and SchedulingEngine services. The test would
> be wider, slower, and harder to prepare for exact failure cases. Real
> compatibility is checked in a targeted integration test, and the complete
> deployment is checked in the system test.

---

## 4. Targeted Integration through RabbitMQ

### What I show

1. the `.csproj` of the shared integration test project;
2. `IntegrationTests/RegistrationNotifications`;
3. `RegistrationNotificationsFixture.cs`;
4. `RegistrationNotificationsIntegrationTests.cs`.

In the `.csproj`, I show `Testcontainers.RabbitMq`,
`Testcontainers.PostgreSql`, `RabbitMQ.Client`, and the project references to the
production producer, consumer, and event contracts.

> Testcontainers manages RabbitMQ and PostgreSQL. The fixture uses
> `RabbitMQ.Client` to check queue readiness. Production project references let
> us test the real `EventPublisher` and consumer without copying their code.

### I draw the boundary

```text
real EventPublisher
  → RabbitMQ Testcontainer
    → real RegistrationEventConsumer inside NotificationsAccessor
      → PostgreSQL Testcontainer
        → GET NotificationsAccessor API
```

Outside the boundary:

```text
Gateway, RegistrationsManager endpoint, DataAccessor, SchedulingEngine, Web UI
```

### What I say

> Now we ask a different question. We are not checking the Manager's business
> decision. We are checking whether the real producer and consumer work together
> through a real broker. A spy is not enough for this.

> We use the production `EventPublisher`, real RabbitMQ, the real versioned
> `RegistrationCreatedV1` event, the real hosted consumer, and a real write from
> NotificationsAccessor to PostgreSQL.

> This is a targeted integration test. It is wider than one component, but it
> does not start the whole system. If it fails, we can focus on messaging instead
> of checking every service.

### Why there are two Testcontainers

> RabbitMQ is the transport we test. PostgreSQL stores the consumer's observable
> result. They start in parallel with `Task.WhenAll` because they do not depend
> on each other during startup.

### Why we wait for consumer readiness

I show `WaitForConsumerAsync`.

> “RabbitMQ is running” does not mean “the consumer has declared its queue and
> subscribed.” Publishing too early can make the test unstable. The fixture
> checks the real queue and waits until it has a consumer.

> A fixed `Task.Delay(3000)` is not reliable. It wastes time on a fast machine
> and may be too short in CI. We wait for a real condition with a timeout.

### Why the result also uses polling

I show `WaitForNotificationAsync`.

> Message delivery is asynchronous. `PublishAsync` confirms publication, not
> completion of the consumer. The test polls the read API until the record
> appears. The wait has a timeout, so the test cannot hang forever.

> We search by a unique `attendeeId`. We do not read the latest notification or
> assume that the database is empty. This example has one test and disposable
> containers. A larger suite would need Respawn, unique correlation IDs, or
> another isolation strategy.

### I show the assertions

- `attendeeId` matches the event;
- the channel is `Email`;
- the subject contains the meeting title;
- the body contains `registrationId`;
- `SentAt` is set after processing.

### I run the test

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=Integration
```

### Can we test retry here?

> Yes. Retry, dead-letter routing, redelivery, idempotency, and temporary errors
> are good messaging integration scenarios because they depend on real broker
> and consumer configuration. Each should be a separate focused test.

> For retry, we need a controlled temporary error and a way to observe attempts
> or the dead-letter queue. A long delay is not a useful retry assertion. We
> should only add such tests when retry is part of the production design.

---

## 5. System Test of the Complete Registration Flow

### What I show

1. `System/SystemIntegrationFixture.cs`;
2. `System/SystemIntegrationTests.cs`;
3. `docker-compose.system-tests.yml`;
4. test-only endpoint mappings in the two Accessor `Program.cs` files.

> The system test is in the same xUnit project, but it does not start
> Testcontainers. Its fixture creates normal `HttpClient` instances for an
> already running Docker Compose environment.

### First, I define the system boundary

```text
test → Gateway → RegistrationsManager → DataAccessor → PostgreSQL
                              ├→ SchedulingEngine
                              └→ RabbitMQ → NotificationsAccessor → PostgreSQL
```

### What I say

> We are no longer testing one component or one integration point. We want to
> know whether a critical backend flow works in the complete deployed system.
> A system client does not have to be a browser. For this backend system test,
> entering through the public Gateway is enough. The UI can have its own browser
> E2E tests if that risk is important.

> Unlike the component tests, this fixture does not start services with
> `WebApplicationFactory` and does not build the topology with Testcontainers.
> It connects to a local Docker Compose environment that is already running.
> This makes it easy to run or debug one test from VS Code and inspect service
> logs separately.

### Why Compose is not started by the xUnit fixture

> Starting Compose from a fixture is possible, but it can be inconvenient for a
> full stack. An IDE may create several testhost processes, ports can conflict,
> cleanup after a crash is harder, and an infrastructure error looks like a
> fixture failure.

> Here, the environment lifecycle is external. A developer starts it locally,
> and a CI workflow starts it in CI. The fixture checks readiness and creates
> clients, but it does not orchestrate Docker.

> Another valid option is a script that runs Compose, waits for readiness, runs
> `dotnet test`, collects logs, and shuts Compose down. We do not require that
> script here because direct IDE execution is useful, but this automation is
> often valuable in CI.

### Normal startup and system-test configuration

Normal environment:

```bash
docker compose up --build
```

Environment with test support:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.system-tests.yml \
  up --build
```

### What I say

> The override file is not loaded automatically and does not create a second
> system. Compose merges the files from left to right. The second file only adds
> `TestSupport__Enabled=true` to two services. Container names, ports, and
> existing volumes stay the same.

> ASP.NET Core converts `TestSupport__Enabled` into the configuration key
> `TestSupport:Enabled`. Without the flag, the test-only routes are not added to
> the endpoint table, so they return `404`.

### What the fixture checks

> The fixture creates clients for Gateway, DataAccessor, and
> NotificationsAccessor. Gateway is the public test boundary. Direct Accessor
> clients are used only to observe the notification and perform technical
> cleanup.

> The fixture then checks health endpoints, test-support routes, and the active
> RabbitMQ consumer. This is a fail-fast check. If the environment is configured
> incorrectly, we get a clear infrastructure error before creating test data.

### An important limitation of this teaching example

> The URLs are currently fixed to `127.0.0.1`. For CI or a cloud deployment,
> they should come from environment variables. The test runner also needs
> network access to internal Accessor services. These services normally should
> not be exposed through public production ingress.

### Setup through public endpoints

I show the creation calls:

1. `POST /venues`;
2. `POST /meetings`;
3. `POST /attendees`.

### What I say

> The test creates prerequisites through the public Gateway: first a venue,
> then its meeting, and then an attendee. These are normal product operations,
> so the system test uses the same contracts as a real client.

> This setup does not depend on database tables. Assertions for prerequisite
> creation are minimal because this is setup, not three separate CRUD tests.

> Names and email addresses contain a unique scenario ID. The test does not
> depend on seed data, does not need an empty database, and does not conflict
> with existing local records. The test changes and deletes only data that it
> created itself.

### Act and synchronous assertions

> The main action is `POST /registrations` through Gateway. We check `201`, the
> server-generated ID, references to the created meeting and attendee, the
> normalized ticket type, and payment status.

> We then use a separate public GET to read registrations for the meeting. This
> proves persistence through an observable API, not only through the POST
> response.

### The asynchronous result

> The notification is not created inside the HTTP transaction. The test polls
> until it finds a notification for the specific attendee and also checks the
> registration ID in the body. Unique values prevent a false success caused by
> an old notification.

> A real product may not expose a public notification read API. Other options
> include an email sandbox, an event audit API, observability storage, or an
> internal test probe. The choice depends on which result the system can expose
> safely.

### Cleanup and dependency order

I show the `finally` block.

```text
notifications
  → registrations
    → attendee
    → meeting
      → venue
```

### What I say

> Cleanup is inside `finally`, so it runs after successful assertions and after
> an exception. The cleanup order is visible in the test, which is useful when
> data has dependencies across several APIs.

> Dependent records are deleted before their parent records. Otherwise, deleting
> an attendee or meeting correctly returns `409 Conflict`.

> `TryDeleteAsync` collects cleanup errors instead of stopping after the first
> one. If notification deletion fails, the test still tries to delete the
> registration, attendee, meeting, and venue. At the end, the errors are joined
> in an `AggregateException`.

### Why this example uses test-only endpoints

I show conditional endpoint registration:

```csharp
if (app.Configuration.GetValue<bool>("TestSupport:Enabled"))
{
    app.MapDelete("/_test/...", ...);
}
```

### What I say

> Deleting a venue, meeting, or attendee is a normal product operation, so those
> endpoints are public. Deleting a sent notification or one registration has no
> separate product or admin meaning in this example. We do not expose those
> operations through Gateway only for this test.

> Their owning Accessors have optional test-support routes. They are not proxied
> through Gateway, they are enabled only by configuration, and they are
> idempotent: repeated cleanup safely returns `204`.

> If suitable CRUD operations already exist, it is fine to use them for test
> setup and cleanup. They use the same contracts as real clients and do not need
> a separate test API.

> Product CRUD may not be enough. A normal endpoint may use soft delete, may not
> allow deletion of a completed registration, or may not support deletion of a
> sent notification. A repeatable test may need a real hard delete of technical
> records.

> Another project may add a real delete or admin operation if it is useful for
> support or a product scenario. Here, I show the optional test-support approach.
> A feature flag alone is not enough: the environment must be isolated and these
> routes must not be available to production traffic.

### Short cleanup alternatives

> Other options are public delete or admin operations, a disposable environment
> for each test run, or direct cleanup of a test database. Direct database
> cleanup is simple, but it couples the system test to the database schema.
> Large suites can also clean all records by test-run ID or tenant ID. In every
> option, the test must delete only the data it owns.

### I run the system test

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=System
```

After Compose is running, I can also run one test from the VS Code Testing panel.

---

## End of the Practical Demo

### What I show

- green results for all five examples in the Testing panel;
- stopped Testcontainers in Docker Desktop;
- the running Compose environment used by the system test.

### What I say

> We have seen all examples in code: application startup with
> `WebApplicationFactory`, PostgreSQL and RabbitMQ with Testcontainers, database
> cleanup with Respawn, HTTP stubs with WireMock, an event spy, fixed time, and a
> system test connected to Docker Compose. We can now discuss implementation
> details or run one of the tests in debug mode.
