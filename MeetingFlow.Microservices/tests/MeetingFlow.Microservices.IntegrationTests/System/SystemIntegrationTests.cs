using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using NotificationsAccessor.Contracts;
using RegistrationsManager.Contracts;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.System;

public sealed class SystemIntegrationTests(SystemIntegrationFixture fixture)
    : IClassFixture<SystemIntegrationFixture>
{
    [Fact]
    [Trait("Category", "System")]
    public async Task CreateRegistration_ThroughGateway_PersistsAndSendsNotification()
    {
        Guid? venueId = null;
        Guid? meetingId = null;
        Guid? attendeeId = null;
        var scenarioId = Guid.NewGuid();
        var meetingTitle = $"System Test Meeting {scenarioId:N}";

        try
        {
            // Arrange through real public use cases. Every record is uniquely
            // owned by this test, so seed data and execution order are irrelevant.
            venueId = (await PostCreatedAsync<CreatedResource>(
                "/venues",
                new
                {
                    Name = $"System Test Venue {scenarioId:N}",
                    Address = "1 System Test Street",
                    City = "Test City",
                    Capacity = 10
                })).Id;

            var startsAt = DateTimeOffset.UtcNow.AddDays(30);
            meetingId = (await PostCreatedAsync<CreatedResource>(
                "/meetings",
                new
                {
                    Title = meetingTitle,
                    Description = "Owned by an isolated MeetingFlow system test.",
                    Status = "Published",
                    StartsAt = startsAt,
                    EndsAt = startsAt.AddHours(2),
                    VenueId = venueId.Value
                })).Id;

            attendeeId = (await PostCreatedAsync<CreatedResource>(
                "/attendees",
                new
                {
                    FullName = "System Test Attendee",
                    Email = $"system-test-{scenarioId:N}@meetingflow.test",
                    Phone = (string?)null,
                    Company = "MeetingFlow"
                })).Id;

            // Act: the business flow also enters through the public Gateway.
            using var response = await fixture.GatewayClient.PostAsJsonAsync(
                "/registrations",
                new CreateRegistrationRequest(
                    meetingId.Value,
                    attendeeId.Value,
                    "General"));

            // Assert the synchronous result returned by the complete HTTP chain.
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content
                .ReadFromJsonAsync<CreateRegistrationResult>();
            Assert.NotNull(result);
            var registration = result.Registration;

            Assert.NotEqual(Guid.Empty, registration.Id);
            Assert.Equal(meetingId.Value, registration.MeetingId);
            Assert.Equal(attendeeId.Value, registration.AttendeeId);
            Assert.Equal("General", registration.TicketType);
            Assert.Equal("Pending", registration.PaymentStatus);

            // Registration persistence is observable again through Gateway.
            var registrations = await fixture.GatewayClient
                .GetFromJsonAsync<List<RegistrationDto>>(
                    $"/registrations/by-meeting/{meetingId.Value}") ?? [];
            Assert.Contains(registrations, item => item.Id == registration.Id);

            // RabbitMQ delivery is asynchronous. Poll by this test's IDs rather
            // than assuming that the notification database starts empty.
            var notification = await WaitForNotificationAsync(
                attendeeId.Value,
                registration.Id);
            Assert.Equal("Email", notification.Type);
            Assert.Equal(
                $"Registration confirmed: {meetingTitle}",
                notification.Subject);
            Assert.Contains(registration.Id.ToString(), notification.Body);
        }
        finally
        {
            var cleanupFailures = new List<Exception>();

            // Technical records have no public delete use case. Their owning
            // Accessors expose opt-in test-support endpoints instead of leaking
            // cleanup operations through the public Gateway contract.
            if (attendeeId is { } attendeeIdForNotificationCleanup)
            {
                await TryDeleteAsync(
                    fixture.NotificationsClient,
                    $"/_test/notifications/by-attendee/{attendeeIdForNotificationCleanup}",
                    "notification test-support endpoint",
                    cleanupFailures);
            }

            if (attendeeId is { } attendeeIdForRegistrationCleanup)
            {
                await TryDeleteAsync(
                    fixture.DataAccessorClient,
                    $"/_test/registrations/by-attendee/{attendeeIdForRegistrationCleanup}",
                    "registration test-support endpoint",
                    cleanupFailures);
            }

            if (attendeeId is { } attendeeIdForPublicCleanup)
            {
                await TryDeleteAsync(
                    fixture.GatewayClient,
                    $"/attendees/{attendeeIdForPublicCleanup}",
                    "public attendee endpoint",
                    cleanupFailures);
            }

            if (meetingId is { } ownedMeetingId)
            {
                await TryDeleteAsync(
                    fixture.GatewayClient,
                    $"/meetings/{ownedMeetingId}",
                    "public meeting endpoint",
                    cleanupFailures);
            }

            if (venueId is { } ownedVenueId)
            {
                await TryDeleteAsync(
                    fixture.GatewayClient,
                    $"/venues/{ownedVenueId}",
                    "public venue endpoint",
                    cleanupFailures);
            }

            if (cleanupFailures.Count > 0)
            {
                throw new AggregateException(
                    "One or more system-test cleanup operations failed.",
                    cleanupFailures);
            }
        }
    }

    private async Task<T> PostCreatedAsync<T>(string path, object request)
    {
        using var response = await fixture.GatewayClient.PostAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException(
                $"POST '{path}' returned an empty response body.");
    }

    private static async Task TryDeleteAsync(
        HttpClient client,
        string path,
        string endpointDescription,
        ICollection<Exception> failures)
    {
        try
        {
            using var response = await client.DeleteAsync(path);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }

            failures.Add(new InvalidOperationException(
                $"Cleanup through the {endpointDescription} failed with "
                + $"{(int)response.StatusCode} ({response.StatusCode})."));
        }
        catch (Exception exception)
        {
            failures.Add(new InvalidOperationException(
                $"Cleanup through the {endpointDescription} failed.",
                exception));
        }
    }

    private async Task<NotificationDto> WaitForNotificationAsync(
        Guid attendeeId,
        Guid registrationId)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(15))
        {
            var notifications = await fixture.NotificationsClient
                .GetFromJsonAsync<List<NotificationDto>>(
                    $"/notifications/by-attendee/{attendeeId}") ?? [];

            var notification = notifications.FirstOrDefault(item =>
                item.Body.Contains(
                    registrationId.ToString(),
                    StringComparison.OrdinalIgnoreCase));
            if (notification is not null)
            {
                return notification;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Notification for attendee '{attendeeId}' and registration "
            + $"'{registrationId}' was not created in time.");
    }

    private sealed record CreatedResource(Guid Id);
}
