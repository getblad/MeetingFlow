using System.Net;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.System;

public sealed class SystemIntegrationFixture : IAsyncLifetime
{
    private const string NotificationQueue = "notifications.registration-created";
    private static readonly Uri GatewayUrl = new("http://127.0.0.1:8080");
    private static readonly Uri DataAccessorUrl = new("http://127.0.0.1:5010");
    private static readonly Uri NotificationsUrl = new("http://127.0.0.1:5011");
    private static readonly Uri RabbitMqUrl = new("amqp://guest:guest@127.0.0.1:5672");

    private HttpClient? _gatewayClient;
    private HttpClient? _dataAccessorClient;
    private HttpClient? _notificationsClient;

    public HttpClient GatewayClient => _gatewayClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public HttpClient DataAccessorClient => _dataAccessorClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public HttpClient NotificationsClient => _notificationsClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        _gatewayClient = new HttpClient { BaseAddress = GatewayUrl };
        _dataAccessorClient = new HttpClient { BaseAddress = DataAccessorUrl };
        _notificationsClient = new HttpClient { BaseAddress = NotificationsUrl };

        await VerifyHealthyAsync(_gatewayClient, "Gateway");
        await VerifyHealthyAsync(_dataAccessorClient, "DataAccessor");
        await VerifyHealthyAsync(_notificationsClient, "NotificationsAccessor");
        await VerifyTestSupportAsync(
            _dataAccessorClient,
            $"/_test/registrations/by-attendee/{Guid.NewGuid()}",
            "DataAccessor");
        await VerifyTestSupportAsync(
            _notificationsClient,
            $"/_test/notifications/by-attendee/{Guid.NewGuid()}",
            "NotificationsAccessor");
        await WaitForNotificationConsumerAsync();
    }

    public Task DisposeAsync()
    {
        _gatewayClient?.Dispose();
        _dataAccessorClient?.Dispose();
        _notificationsClient?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task VerifyHealthyAsync(HttpClient client, string service)
    {
        try
        {
            using var response = await client.GetAsync("/health");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException(
                $"The {service} system-test endpoint at '{client.BaseAddress}' is not ready. "
                + "Start the local backend with 'docker compose up --build' before running the test.",
                exception);
        }
    }

    private static async Task VerifyTestSupportAsync(
        HttpClient client,
        string path,
        string service)
    {
        using var response = await client.DeleteAsync(path);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return;
        }

        throw new InvalidOperationException(
            $"The {service} test-support endpoint is disabled. Start the local "
            + "backend with 'docker compose -f docker-compose.yml "
            + "-f docker-compose.system-tests.yml up --build'.");
    }

    private static async Task WaitForNotificationConsumerAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                var connectionFactory = new ConnectionFactory { Uri = RabbitMqUrl };
                await using var connection =
                    await connectionFactory.CreateConnectionAsync(timeout.Token);
                await using var channel =
                    await connection.CreateChannelAsync(cancellationToken: timeout.Token);

                await channel.QueueDeclarePassiveAsync(NotificationQueue, timeout.Token);
                if (await channel.ConsumerCountAsync(NotificationQueue, timeout.Token) > 0)
                {
                    return;
                }
            }
            catch (OperationInterruptedException)
            {
                // The local consumer has not declared its queue yet.
            }
            catch (BrokerUnreachableException)
            {
                // RabbitMQ is running locally but is not accepting connections yet.
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(100, timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException(
            $"RabbitMQ consumer did not subscribe to '{NotificationQueue}'. "
            + "Make sure the local NotificationsAccessor is running.");
    }
}
