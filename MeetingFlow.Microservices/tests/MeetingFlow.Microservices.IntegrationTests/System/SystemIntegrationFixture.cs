using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.System;

public sealed class SystemIntegrationFixture : IAsyncLifetime
{
    private const string GatewayUrlVariable = "MEETINGFLOW_SYSTEM_GATEWAY_URL";
    private const string NotificationsUrlVariable =
        "MEETINGFLOW_SYSTEM_NOTIFICATIONS_URL";

    private HttpClient? _gatewayClient;
    private HttpClient? _notificationsClient;

    public HttpClient GatewayClient => _gatewayClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public HttpClient NotificationsClient => _notificationsClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        var gatewayUrl = GetRequiredUrl(GatewayUrlVariable);
        var notificationsUrl = GetRequiredUrl(NotificationsUrlVariable);

        _gatewayClient = new HttpClient { BaseAddress = gatewayUrl };
        _notificationsClient = new HttpClient { BaseAddress = notificationsUrl };

        await VerifyHealthyAsync(_gatewayClient, "Gateway");
        await VerifyHealthyAsync(_notificationsClient, "NotificationsAccessor");
    }

    public Task DisposeAsync()
    {
        _gatewayClient?.Dispose();
        _notificationsClient?.Dispose();
        return Task.CompletedTask;
    }

    private static Uri GetRequiredUrl(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Environment variable '{variableName}' must contain an absolute URL. "
                + "Start the system-test environment with "
                + "'MeetingFlow.Microservices/tests/run-system-tests.sh' instead of "
                + "running this test directly.");
        }

        return uri;
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
                + "The fixture validates the environment but does not start Docker Compose.",
                exception);
        }
    }
}
