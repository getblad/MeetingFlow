using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace MeetingFlow.DataAccessor.ComponentTests;

public sealed class DataAccessorFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("meetingflow_component_tests")
        .WithUsername("meetingflow")
        .WithPassword("meetingflow")
        .Build();

    private WebApplicationFactory<Program>? _application;
    private HttpClient? _client;

    public HttpClient Client => _client
        ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("POSTGRES_CONN", _postgres.GetConnectionString());
            });

        // CreateClient starts DataAccessor. Its normal startup path creates the
        // schema and inserts seed data into this disposable PostgreSQL database.
        _client = _application.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _application?.Dispose();
        await _postgres.DisposeAsync();
    }
}
