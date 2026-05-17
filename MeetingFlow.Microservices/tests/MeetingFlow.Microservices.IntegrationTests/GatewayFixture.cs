using System.Net.Http.Json;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests;

// Shared per-collection fixture. Points HttpClient at the gateway exposed by
// `docker compose up` and pings /health so we fail fast with a clear message
// when the stack isn't running, instead of hundreds of confusing assertion errors.
public sealed class GatewayFixture : IAsyncLifetime
{
    public HttpClient Http { get; } = new() { BaseAddress = new Uri("http://localhost:8080/") };
    public bool StackUp { get; private set; }
    public string? StackDownReason { get; private set; }

    public async Task InitializeAsync()
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var resp = await Http.GetAsync("health");
                if (resp.IsSuccessStatusCode) { StackUp = true; return; }
            }
            catch { /* swallow connection failures while waiting */ }
            await Task.Delay(500);
        }
        StackDownReason = "Gateway at http://localhost:8080/health did not respond within 10s. " +
                          "Run `docker compose up --build` from MeetingFlow.Microservices/ first.";
    }

    public Task DisposeAsync()
    {
        Http.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition(nameof(GatewayCollection))]
public sealed class GatewayCollection : ICollectionFixture<GatewayFixture> { }
