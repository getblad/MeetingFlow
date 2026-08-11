using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.System;

public sealed class SystemIntegrationFixture : IAsyncLifetime
{
    private const string NotificationQueue = "notifications.registration-created";
    private static readonly Uri GatewayUrl = new("http://127.0.0.1:8080");
    private static readonly Uri NotificationsUrl = new("http://127.0.0.1:5011");
    private static readonly Uri RabbitMqUrl = new("amqp://guest:guest@127.0.0.1:5672");
    private const string PostgresConnectionString =
        "Host=127.0.0.1;Port=5432;Database=meetingflow;Username=meetingflow;Password=meetingflow";

    private HttpClient? _gatewayClient;
    private HttpClient? _notificationsClient;

    public HttpClient GatewayClient => _gatewayClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public HttpClient NotificationsClient => _notificationsClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public string DatabaseConnectionString => PostgresConnectionString;

    public async Task InitializeAsync()
    {
        _gatewayClient = new HttpClient { BaseAddress = GatewayUrl };
        _notificationsClient = new HttpClient { BaseAddress = NotificationsUrl };

        await VerifyHealthyAsync(_gatewayClient, "Gateway");
        await VerifyHealthyAsync(_notificationsClient, "NotificationsAccessor");
        await WaitForNotificationConsumerAsync();
    }

    public Task DisposeAsync()
    {
        _gatewayClient?.Dispose();
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
