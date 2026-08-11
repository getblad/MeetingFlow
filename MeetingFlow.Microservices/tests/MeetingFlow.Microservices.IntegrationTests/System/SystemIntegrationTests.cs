using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.System;

public sealed class SystemIntegrationTests(SystemIntegrationFixture fixture)
    : IClassFixture<SystemIntegrationFixture>
{
    private static readonly Guid MeetingId =
        Guid.Parse("b2000000-0000-0000-0000-000000000002");

    private static readonly Guid AttendeeId =
        Guid.Parse("e5000000-0000-0000-0000-000000000015");

    [Fact]
    [Trait("Category", "System")]
    public async Task CreateRegistration_ThroughGateway_PersistsAndSendsNotification()
    {
        // The test expects a clean local Compose environment with seed data.
        Assert.Empty(await GetNotificationsAsync());

        // Act: the only action enters through the public backend boundary.
        using var response = await fixture.GatewayClient.PostAsJsonAsync(
            "/registrations",
            new
            {
                meetingId = MeetingId,
                attendeeId = AttendeeId,
                ticketType = "General"
            });

        // Assert the synchronous result returned by the complete HTTP chain.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var responseJson = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var registration = responseJson.RootElement.GetProperty("registration");
        var registrationId = registration.GetProperty("id").GetGuid();

        Assert.NotEqual(Guid.Empty, registrationId);
        Assert.Equal(MeetingId, registration.GetProperty("meetingId").GetGuid());
        Assert.Equal(AttendeeId, registration.GetProperty("attendeeId").GetGuid());
        Assert.Equal("General", registration.GetProperty("ticketType").GetString());
        Assert.Equal("Pending", registration.GetProperty("paymentStatus").GetString());

        // Registration persistence is observable again through the Gateway.
        using var registrationsResponse = await fixture.GatewayClient.GetAsync(
            $"/registrations/by-meeting/{MeetingId}");
        registrationsResponse.EnsureSuccessStatusCode();
        using var registrationsJson = JsonDocument.Parse(
            await registrationsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            registrationsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == registrationId);

        // RabbitMQ delivery is asynchronous. Poll the notification read API
        // until the externally observable side effect appears.
        var notification = await WaitForNotificationAsync();
        Assert.Equal("Email", notification.GetProperty("type").GetString());
        Assert.Equal(
            "Registration confirmed: Cloud Integration Day",
            notification.GetProperty("subject").GetString());
        Assert.Contains(
            registrationId.ToString(),
            notification.GetProperty("body").GetString());
    }

    private async Task<JsonElement> WaitForNotificationAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(15))
        {
            var notifications = await GetNotificationsAsync();
            if (notifications.Count > 0)
            {
                return Assert.Single(notifications);
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Notification for attendee '{AttendeeId}' was not created in time.");
    }

    private async Task<List<JsonElement>> GetNotificationsAsync()
    {
        using var response = await fixture.NotificationsClient.GetAsync(
            $"/notifications/by-attendee/{AttendeeId}");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return json.RootElement
            .EnumerateArray()
            .Select(item => item.Clone())
            .ToList();
    }
}
