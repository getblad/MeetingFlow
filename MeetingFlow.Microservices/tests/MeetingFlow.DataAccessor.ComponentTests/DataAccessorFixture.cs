using DataAccessor.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
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
    private Respawner? _respawner;

    public HttpClient Client => _client
        ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public async Task SeedAsync<TEntity>(params TEntity[] entities)
        where TEntity : class
    {
        if (_application is null)
        {
            throw new InvalidOperationException("The fixture has not been initialized.");
        }

        using var scope = _application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeetingFlowDbContext>();

        db.Set<TEntity>().AddRange(entities);
        await db.SaveChangesAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("The fixture has not been initialized.");
        }

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

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
        // schema and inserts production seed data. Respawn removes that data
        // before every test, so tests only observe their own Arrange data.
        _client = _application.CreateClient();

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["meetings", "registrations", "feedback"]
        });
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _application?.Dispose();
        await _postgres.DisposeAsync();
    }
}
