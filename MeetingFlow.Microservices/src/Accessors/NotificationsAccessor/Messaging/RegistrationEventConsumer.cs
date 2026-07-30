using System.Text;
using System.Text.Json;
using MeetingFlow.IntegrationEvents;
using NotificationsAccessor.Data;
using NotificationsAccessor.Infrastructure;
using NotificationsAccessor.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationsAccessor.Messaging;

public class RegistrationEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _rabbitUrl;
    private readonly ILogger<RegistrationEventConsumer> _logger;
    private readonly FakeSmtpGateway _smtp;
    private const string ExchangeName = "meetingflow.events";
    private const string QueueName = "notifications.registration-created";
    private const string RoutingKey = "registration.created.v1";

    public RegistrationEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<RegistrationEventConsumer> logger,
        FakeSmtpGateway smtp)
    {
        _scopeFactory = scopeFactory;
        _rabbitUrl = config["RABBITMQ_URL"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_URL")
            ?? "amqp://guest:guest@localhost:5672";
        _logger = logger;
        _smtp = smtp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry connection to RabbitMQ (it may start after this service).
        IConnection? connection = null;
        for (var i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
        {
            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(_rabbitUrl) };
                connection = await factory.CreateConnectionAsync(stoppingToken);
                break;
            }
            catch
            {
                _logger.LogWarning("RabbitMQ not ready, retrying in 3s...");
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (connection is null) return;

        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            QueueName,
            ExchangeName,
            RoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<RegistrationCreatedV1>(json);
                if (evt is not null)
                {
                    await HandleEventAsync(evt);
                }
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process registration.created event");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

        // Keep running until cancelled.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleEventAsync(RegistrationCreatedV1 evt)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            AttendeeId = evt.AttendeeId,
            Type = "Email",
            Subject = $"Registration confirmed: {evt.MeetingTitle}",
            Body = $"You have been registered for '{evt.MeetingTitle}'. Registration ID: {evt.RegistrationId}",
            RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(evt),
            SentAt = DateTimeOffset.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        await _smtp.SendAsync(
            evt.RecipientEmail,
            notification.Subject,
            notification.Body,
            notification.RawPayloadJson);

        _logger.LogInformation("Processed registration.created event for {RegistrationId}", evt.RegistrationId);
    }
}
