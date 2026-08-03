using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests.System;

public sealed class SystemIntegrationFixture : IAsyncLifetime
{
    private static readonly string[] PortVariables =
    [
        "POSTGRES_HOST_PORT",
        "RABBITMQ_HOST_PORT",
        "RABBITMQ_MANAGEMENT_HOST_PORT",
        "DATAACCESSOR_HOST_PORT",
        "NOTIFICATIONS_ACCESSOR_HOST_PORT",
        "SCHEDULING_ENGINE_HOST_PORT",
        "AI_CHAT_ENGINE_HOST_PORT",
        "MEETINGS_MANAGER_HOST_PORT",
        "REGISTRATIONS_MANAGER_HOST_PORT",
        "GATEWAY_HOST_PORT",
        "WEB_HOST_PORT"
    ];

    private readonly string _composeDirectory = FindComposeDirectory();
    private readonly string _projectName =
        $"meetingflow-system-{Guid.NewGuid():N}";
    private readonly Dictionary<string, string> _environment =
        PortVariables.ToDictionary(name => name, _ => "0");

    private HttpClient? _gatewayClient;
    private HttpClient? _notificationsClient;
    private bool _stackWasStarted;

    public HttpClient GatewayClient => _gatewayClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public HttpClient NotificationsClient => _notificationsClient
        ?? throw new InvalidOperationException("The system fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        _stackWasStarted = true;

        try
        {
            await RunComposeCheckedAsync(
                TimeSpan.FromMinutes(10),
                "up",
                "--build",
                "--detach",
                "--wait",
                "--wait-timeout",
                "180");

            var servicePorts = new Dictionary<string, int>
            {
                ["dataaccessor"] = await GetHostPortAsync("dataaccessor", 5010),
                ["notifications-accessor"] = await GetHostPortAsync("notifications-accessor", 5011),
                ["scheduling-engine"] = await GetHostPortAsync("scheduling-engine", 5020),
                ["ai-chat-engine"] = await GetHostPortAsync("ai-chat-engine", 5040),
                ["meetings-manager"] = await GetHostPortAsync("meetings-manager", 5030),
                ["registrations-manager"] = await GetHostPortAsync("registrations-manager", 5031),
                ["gateway"] = await GetHostPortAsync("gateway", 8080)
            };

            foreach (var (service, port) in servicePorts)
            {
                await WaitForHttpAsync(
                    new Uri($"http://127.0.0.1:{port}/health"),
                    service,
                    TimeSpan.FromSeconds(45));
            }

            await WaitForNotificationConsumerAsync(TimeSpan.FromSeconds(30));

            _gatewayClient = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{servicePorts["gateway"]}")
            };
            _notificationsClient = new HttpClient
            {
                BaseAddress = new Uri(
                    $"http://127.0.0.1:{servicePorts["notifications-accessor"]}")
            };
        }
        catch (Exception exception)
        {
            var logs = await RunComposeAsync(
                TimeSpan.FromSeconds(30),
                "logs",
                "--no-color",
                "--tail",
                "200");

            await StopStackAsync();
            throw new InvalidOperationException(
                $"Could not start the MeetingFlow system test stack.\n{logs.StandardOutput}\n{logs.StandardError}",
                exception);
        }
    }

    public async Task DisposeAsync()
    {
        _gatewayClient?.Dispose();
        _notificationsClient?.Dispose();
        await StopStackAsync();
    }

    private async Task<int> GetHostPortAsync(string service, int containerPort)
    {
        var result = await RunComposeCheckedAsync(
            TimeSpan.FromSeconds(20),
            "port",
            service,
            containerPort.ToString());

        var endpoint = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First();
        var separator = endpoint.LastIndexOf(':');

        return separator >= 0
            && int.TryParse(endpoint[(separator + 1)..], out var port)
                ? port
                : throw new InvalidOperationException(
                    $"Could not parse host port from '{endpoint}'.");
    }

    private async Task WaitForNotificationConsumerAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        var consumerPattern = new Regex(
            @"notifications\.registration-created\s+[1-9]\d*",
            RegexOptions.CultureInvariant);

        while (stopwatch.Elapsed < timeout)
        {
            var result = await RunComposeAsync(
                TimeSpan.FromSeconds(15),
                "exec",
                "-T",
                "rabbitmq",
                "rabbitmqctl",
                "list_queues",
                "name",
                "consumers",
                "--quiet");

            if (result.ExitCode == 0
                && consumerPattern.IsMatch(result.StandardOutput))
            {
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            "NotificationsAccessor did not subscribe to the registration queue in time.");
    }

    private static async Task WaitForHttpAsync(
        Uri endpoint,
        string service,
        TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                using var response = await client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // The container is running, but Kestrel is not listening yet.
            }
            catch (TaskCanceledException)
            {
                // A single health request timed out; retry until the outer deadline.
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Service '{service}' did not become healthy at '{endpoint}' in time.");
    }

    private async Task StopStackAsync()
    {
        if (!_stackWasStarted)
        {
            return;
        }

        _stackWasStarted = false;
        await RunComposeAsync(
            TimeSpan.FromMinutes(2),
            "down",
            "--volumes",
            "--remove-orphans",
            "--timeout",
            "10");
    }

    private Task<CommandResult> RunComposeCheckedAsync(
        TimeSpan timeout,
        params string[] arguments) =>
        RunComposeAsync(timeout, throwOnError: true, arguments);

    private Task<CommandResult> RunComposeAsync(
        TimeSpan timeout,
        params string[] arguments) =>
        RunComposeAsync(timeout, throwOnError: false, arguments);

    private async Task<CommandResult> RunComposeAsync(
        TimeSpan timeout,
        bool throwOnError,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = _composeDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("compose");
        startInfo.ArgumentList.Add("--project-name");
        startInfo.ArgumentList.Add(_projectName);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in _environment)
        {
            startInfo.Environment[name] = value;
        }
        startInfo.Environment["COMPOSE_ANSI"] = "never";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Docker Compose.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Docker Compose did not finish within {timeout}.");
        }

        var result = new CommandResult(
            process.ExitCode,
            await standardOutput,
            await standardError);

        if (throwOnError && result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Docker Compose exited with code {result.ExitCode}.\n{result.StandardOutput}\n{result.StandardError}");
        }

        return result;
    }

    private static string FindComposeDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find MeetingFlow.Microservices/docker-compose.yml.");
    }

    private sealed record CommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
