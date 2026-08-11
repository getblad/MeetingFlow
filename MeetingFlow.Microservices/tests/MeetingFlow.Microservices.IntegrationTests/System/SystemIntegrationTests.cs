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
        // Arrange: create only this test's prerequisites directly in the real
        // local database. The production API does not need artificial CRUD
        // endpoints solely to support test setup and cleanup.
        await using var scenario = await RegistrationTestDataScope.CreateAsync(
            fixture.DatabaseConnectionString);

        // Act: the only tested business action enters through the public backend boundary.
        using var response = await fixture.GatewayClient.PostAsJsonAsync(
            "/registrations",
            new CreateRegistrationRequest(
                scenario.MeetingId,
                scenario.AttendeeId,
                "General"));

        // Assert the synchronous result returned by the complete HTTP chain.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateRegistrationResult>();
        Assert.NotNull(result);
        var registration = result.Registration;

        Assert.NotEqual(Guid.Empty, registration.Id);
        Assert.Equal(scenario.MeetingId, registration.MeetingId);
        Assert.Equal(scenario.AttendeeId, registration.AttendeeId);
        Assert.Equal("General", registration.TicketType);
        Assert.Equal("Pending", registration.PaymentStatus);

        // Registration persistence is observable again through the Gateway.
        var registrations = await fixture.GatewayClient
            .GetFromJsonAsync<List<RegistrationDto>>(
                $"/registrations/by-meeting/{scenario.MeetingId}") ?? [];
        Assert.Contains(registrations, item => item.Id == registration.Id);

        // RabbitMQ delivery is asynchronous. Poll the notification read API
        // for this test's correlation IDs rather than assuming an empty database.
        var notification = await WaitForNotificationAsync(
            scenario.AttendeeId,
            registration.Id);
        Assert.Equal("Email", notification.Type);
        Assert.Equal(
            $"Registration confirmed: {scenario.MeetingTitle}",
            notification.Subject);
        Assert.Contains(registration.Id.ToString(), notification.Body);
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
}
