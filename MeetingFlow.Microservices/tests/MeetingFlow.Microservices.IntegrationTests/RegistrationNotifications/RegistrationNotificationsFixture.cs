extern alias NotificationsApp;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RegistrationsManager.Messaging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;
using NotificationsProgram = NotificationsApp::Program;

namespace MeetingFlow.Microservices.IntegrationTests.RegistrationNotifications;

public sealed class RegistrationNotificationsFixture : IAsyncLifetime
{
    private const string QueueName = "notifications.registration-created";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("meetingflow_integration_tests")
        .WithUsername("meetingflow")
        .WithPassword("meetingflow")
        .Build();

    private readonly RabbitMqContainer _rabbitMq =
        new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .WithUsername("meetingflow")
            .WithPassword("meetingflow")
            .Build();

    private WebApplicationFactory<NotificationsProgram>? _application;
    private HttpClient? _client;
    private EventPublisher? _publisher;

    public HttpClient Client => _client
        ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public EventPublisher Publisher => _publisher
        ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _rabbitMq.StartAsync());

        _application = new WebApplicationFactory<NotificationsProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("POSTGRES_CONN", _postgres.GetConnectionString());
                builder.UseSetting("RABBITMQ_URL", _rabbitMq.GetConnectionString());
            });

        // Starting the HTTP host also starts RegistrationEventConsumer.
        _client = _application.CreateClient();

        await WaitForConsumerAsync();
        _publisher = await EventPublisher.CreateAsync(_rabbitMq.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (_publisher is not null)
        {
            await _publisher.DisposeAsync();
        }

        _client?.Dispose();
        _application?.Dispose();
        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task WaitForConsumerAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                var connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(_rabbitMq.GetConnectionString())
                };

                await using var connection =
                    await connectionFactory.CreateConnectionAsync(timeout.Token);
                await using var channel =
                    await connection.CreateChannelAsync(cancellationToken: timeout.Token);

                await channel.QueueDeclarePassiveAsync(QueueName, timeout.Token);
                if (await channel.ConsumerCountAsync(QueueName, timeout.Token) > 0)
                {
                    return;
                }
            }
            catch (OperationInterruptedException)
            {
                // The hosted consumer has not declared its queue yet.
            }
            catch (BrokerUnreachableException)
            {
                // RabbitMQ has started but is not accepting connections yet.
            }

            await Task.Delay(100, timeout.Token);
        }

        throw new TimeoutException(
            $"RabbitMQ consumer did not subscribe to '{QueueName}' in time.");
    }
}
