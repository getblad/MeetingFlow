using RegistrationsManager.Messaging;

namespace MeetingFlow.RegistrationsManager.ComponentTests;

public sealed record PublishedEvent(string RoutingKey, object Message);

public sealed class SpyEventPublisher : IEventPublisher
{
    private readonly List<PublishedEvent> _events = [];
    private readonly object _lock = new();

    public IReadOnlyList<PublishedEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.ToList();
            }
        }
    }

    public Task PublishAsync<T>(string routingKey, T message)
    {
        lock (_lock)
        {
            _events.Add(new PublishedEvent(routingKey, message!));
        }

        return Task.CompletedTask;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
