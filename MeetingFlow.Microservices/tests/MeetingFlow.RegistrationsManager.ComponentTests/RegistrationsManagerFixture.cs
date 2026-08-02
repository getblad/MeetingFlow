using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RegistrationsManager.Messaging;
using WireMock.Server;

namespace MeetingFlow.RegistrationsManager.ComponentTests;

public sealed class RegistrationsManagerFixture : IDisposable
{
    private readonly WebApplicationFactory<Program> _application;

    public WireMockServer DataAccessorStub { get; } = WireMockServer.Start();
    public WireMockServer SchedulingEngineStub { get; } = WireMockServer.Start();
    public SpyEventPublisher EventPublisher { get; } = new();
    public HttpClient Client { get; }

    public RegistrationsManagerFixture()
    {
        var fixedTime = new StubTimeProvider(
            DateTimeOffset.Parse("2026-08-01T12:00:00Z"));

        _application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("DATA_ACCESSOR_URL", DataAccessorStub.Url!);
                builder.UseSetting("SCHEDULING_ENGINE_URL", SchedulingEngineStub.Url!);

                builder.ConfigureServices(services =>
                {
                    // Replace RabbitMQ and system time with deterministic test doubles.
                    services.RemoveAll<IEventPublisher>();
                    services.AddSingleton<IEventPublisher>(EventPublisher);
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(fixedTime);
                });
            });

        Client = _application.CreateClient();
    }

    public void Reset()
    {
        DataAccessorStub.Reset();
        SchedulingEngineStub.Reset();
        EventPublisher.Reset();
    }

    public void Dispose()
    {
        Client.Dispose();
        _application.Dispose();
        DataAccessorStub.Dispose();
        SchedulingEngineStub.Dispose();
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
