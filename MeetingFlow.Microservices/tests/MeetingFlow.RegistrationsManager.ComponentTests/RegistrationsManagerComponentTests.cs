using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DataAccessor.Contracts;
using MeetingFlow.IntegrationEvents;
using RegistrationsManager.Contracts;
using SchedulingEngine.Contracts;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using AccessorRegistrationDto = DataAccessor.Contracts.RegistrationDto;

namespace MeetingFlow.RegistrationsManager.ComponentTests;

public sealed class RegistrationsManagerComponentTests(
    RegistrationsManagerFixture fixture)
    : IClassFixture<RegistrationsManagerFixture>
{
    private static readonly Guid MeetingId =
        Guid.Parse("b2000000-0000-0000-0000-000000000002");

    private static readonly Guid AttendeeId =
        Guid.Parse("e5000000-0000-0000-0000-000000000015");

    private static readonly Guid RegistrationId =
        Guid.Parse("f6000000-0000-0000-0000-000000000099");

    [Fact]
    public async Task CreateRegistration_WhenDependenciesSucceed_OrchestratesFlowAndPublishesEvent()
    {
        // Arrange
        fixture.Reset();
        StubMeetingAndAttendee();
        StubRegistrations([]);

        fixture.SchedulingEngineStub
            .Given(Request.Create()
                .WithPath("/scheduling/check-capacity")
                .UsingPost())
            .RespondWith(Json(new CheckCapacityResult(true, 800)));

        var registeredAt = DateTimeOffset.Parse("2026-08-01T12:01:00Z");
        var savedRegistration = new AccessorRegistrationDto(
            RegistrationId,
            MeetingId,
            AttendeeId,
            registeredAt,
            "General",
            "Pending",
            null);

        fixture.DataAccessorStub
            .Given(Request.Create()
                .WithPath("/data/registrations")
                .UsingPost())
            .RespondWith(Json(savedRegistration, HttpStatusCode.Created));

        // Act: lowercase ticket type also checks Manager normalization.
        var response = await fixture.Client.PostAsJsonAsync(
            "/registrations",
            new CreateRegistrationRequest(MeetingId, AttendeeId, "general"));

        // Assert the Manager's public use-case result.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateRegistrationResult>();
        Assert.NotNull(result);
        Assert.Equal(RegistrationId, result.Registration.Id);
        Assert.Equal("General", result.Registration.TicketType);
        Assert.Equal(179.10m, result.CalculatedPrice);

        Assert.Equal(
            new CheckCapacityRequest(800, 0),
            ReadSinglePost<CheckCapacityRequest>(
                fixture.SchedulingEngineStub,
                "/scheduling/check-capacity"));
        Assert.Equal(
            new PersistRegistrationRequest(MeetingId, AttendeeId, "General"),
            ReadSinglePost<PersistRegistrationRequest>(
                fixture.DataAccessorStub,
                "/data/registrations"));

        // RabbitMQ is replaced by a spy, so we assert the integration event
        // without running a broker in this component suite.
        var published = Assert.Single(fixture.EventPublisher.Events);
        Assert.Equal("registration.created.v1", published.RoutingKey);

        var integrationEvent = Assert.IsType<RegistrationCreatedV1>(published.Message);
        Assert.Equal(RegistrationId, integrationEvent.RegistrationId);
        Assert.Equal("Cloud Integration Day", integrationEvent.MeetingTitle);
        Assert.Equal("fatima@example.com", integrationEvent.RecipientEmail);
    }

    [Fact]
    public async Task CreateRegistration_WhenAttendeeAlreadyRegistered_StopsBeforeCapacityCheck()
    {
        // Arrange
        fixture.Reset();
        StubMeetingAndAttendee();
        StubRegistrations(
        [
            new AccessorRegistrationDto(
                Guid.NewGuid(),
                MeetingId,
                AttendeeId,
                DateTimeOffset.Parse("2026-07-01T12:00:00Z"),
                "General",
                "Paid",
                null)
        ]);

        // Act
        var response = await fixture.Client.PostAsJsonAsync(
            "/registrations",
            new CreateRegistrationRequest(MeetingId, AttendeeId, "General"));

        // Assert: no SchedulingEngine stub was configured; reaching it would
        // make the test fail instead of returning the expected conflict.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(fixture.SchedulingEngineStub.LogEntries);
        Assert.Empty(fixture.EventPublisher.Events);
    }

    [Fact]
    public async Task CreateRegistration_WhenMeetingIsFull_DoesNotPersistOrPublish()
    {
        // Arrange
        fixture.Reset();
        StubMeetingAndAttendee(venueCapacity: 1);
        StubRegistrations(
        [
            new AccessorRegistrationDto(
                Guid.NewGuid(),
                MeetingId,
                Guid.NewGuid(),
                DateTimeOffset.Parse("2026-07-01T12:00:00Z"),
                "VIP",
                "Paid",
                null)
        ]);

        fixture.SchedulingEngineStub
            .Given(Request.Create()
                .WithPath("/scheduling/check-capacity")
                .UsingPost())
            .RespondWith(Json(new CheckCapacityResult(false, 0)));

        // Act
        var response = await fixture.Client.PostAsJsonAsync(
            "/registrations",
            new CreateRegistrationRequest(MeetingId, AttendeeId, "General"));

        // Assert: POST /data/registrations has no stub. Calling it would result
        // in a downstream failure, so Conflict proves the flow stopped earlier.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(fixture.EventPublisher.Events);
        Assert.Equal(
            new CheckCapacityRequest(1, 1),
            ReadSinglePost<CheckCapacityRequest>(
                fixture.SchedulingEngineStub,
                "/scheduling/check-capacity"));
        Assert.DoesNotContain(
            fixture.DataAccessorStub.LogEntries,
            entry => entry.RequestMessage?.Path == "/data/registrations"
                     && entry.RequestMessage.Method == "POST");
    }

    private void StubMeetingAndAttendee(int venueCapacity = 800)
    {
        fixture.DataAccessorStub
            .Given(Request.Create()
                .WithPath($"/data/meetings/{MeetingId}/registration-context")
                .UsingGet())
            .RespondWith(Json(new RegistrationMeetingContextDto(
                MeetingId,
                "Cloud Integration Day",
                "Published",
                DateTimeOffset.Parse("2026-11-01T12:00:00Z"),
                venueCapacity)));

        fixture.DataAccessorStub
            .Given(Request.Create()
                .WithPath($"/data/attendees/{AttendeeId}/contact")
                .UsingGet())
            .RespondWith(Json(new AttendeeContactDto(
                AttendeeId,
                "Fatima Al-Rashid",
                "fatima@example.com")));
    }

    private void StubRegistrations(IReadOnlyList<AccessorRegistrationDto> registrations)
    {
        fixture.DataAccessorStub
            .Given(Request.Create()
                .WithPath($"/data/registrations/by-meeting/{MeetingId}")
                .UsingGet())
            .RespondWith(Json(registrations));
    }

    private static IResponseBuilder Json(
        object body,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        Response.Create()
            .WithStatusCode(statusCode)
            .WithHeader("Content-Type", "application/json")
            .WithBodyAsJson(body);

    private static T ReadSinglePost<T>(WireMockServer stub, string path)
    {
        var logEntry = Assert.Single(stub.LogEntries, entry =>
            entry.RequestMessage?.Path == path
            && entry.RequestMessage.Method == "POST");
        var body = Assert.IsType<string>(logEntry.RequestMessage!.Body);

        return JsonSerializer.Deserialize<T>(body, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"Could not deserialize request to {typeof(T).Name}.");
    }
}
