using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace RegistrationsManager.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T message);
}

public class EventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "meetingflow.events";

    private EventPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<EventPublisher> CreateAsync(string rabbitUrl)
    {
        var factory = new ConnectionFactory { Uri = new Uri(rabbitUrl) };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true);
        return new EventPublisher(connection, channel);
    }

    public async Task PublishAsync<T>(string routingKey, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsync(ExchangeName, routingKey, body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
