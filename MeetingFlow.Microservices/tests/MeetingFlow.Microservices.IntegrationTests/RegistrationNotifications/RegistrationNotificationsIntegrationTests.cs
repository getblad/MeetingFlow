using System.Diagnostics;
using System.Net.Http.Json;
using MeetingFlow.IntegrationEvents;
using NotificationsAccessor.Contracts;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.RegistrationNotifications;

public sealed class RegistrationNotificationsIntegrationTests(
    RegistrationNotificationsFixture fixture)
    : IClassFixture<RegistrationNotificationsFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegistrationCreatedEvent_IsDeliveredAndPersistedAsNotification()
    {
        // Arrange
        var attendeeId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var integrationEvent = new RegistrationCreatedV1(
            EventId: Guid.NewGuid(),
            RegistrationId: registrationId,
            MeetingId: Guid.NewGuid(),
            AttendeeId: attendeeId,
            MeetingTitle: "Messaging Integration Workshop",
            RecipientName: "Integration Test Attendee",
            RecipientEmail: "integration@example.com",
            RegisteredAt: DateTimeOffset.Parse("2026-08-02T10:00:00Z"));

        // Act: use the production publisher, not a raw test RabbitMQ client.
        await fixture.Publisher.PublishAsync(
            "registration.created.v1",
            integrationEvent);

        // Assert: consumption is asynchronous, so poll the observable HTTP
        // result instead of relying on a fixed Task.Delay.
        var notification = await WaitForNotificationAsync(attendeeId);

        Assert.Equal(attendeeId, notification.AttendeeId);
        Assert.Equal("Email", notification.Type);
        Assert.Equal(
            "Registration confirmed: Messaging Integration Workshop",
            notification.Subject);
        Assert.Contains(registrationId.ToString(), notification.Body);
        Assert.NotNull(notification.SentAt);
    }

    private async Task<NotificationDto> WaitForNotificationAsync(Guid attendeeId)
    {
        var timeout = Stopwatch.StartNew();

        while (timeout.Elapsed < TimeSpan.FromSeconds(10))
        {
            var notifications =
                await fixture.Client.GetFromJsonAsync<List<NotificationDto>>(
                    $"/notifications/by-attendee/{attendeeId}") ?? [];

            if (notifications.Count > 0)
            {
                return Assert.Single(notifications);
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"Notification for attendee '{attendeeId}' was not created in time.");
    }
}
