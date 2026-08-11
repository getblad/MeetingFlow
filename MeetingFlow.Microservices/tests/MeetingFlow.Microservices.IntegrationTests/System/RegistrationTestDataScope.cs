using Npgsql;

namespace MeetingFlow.Microservices.IntegrationTests.System;

/// <summary>
/// Owns the prerequisite records for one system-test scenario. It prepares and
/// removes data directly because those operations are test plumbing, not public
/// MeetingFlow use cases. The behavior under test still enters through Gateway.
/// </summary>
public sealed class RegistrationTestDataScope : IAsyncDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public Guid VenueId { get; } = Guid.NewGuid();
    public Guid MeetingId { get; } = Guid.NewGuid();
    public Guid AttendeeId { get; } = Guid.NewGuid();
    public string MeetingTitle { get; }

    private RegistrationTestDataScope(string connectionString)
    {
        EnsureLocalDatabase(connectionString);
        _connectionString = connectionString;
        MeetingTitle = $"System Test Meeting {MeetingId:N}";
    }

    public static async Task<RegistrationTestDataScope> CreateAsync(
        string connectionString)
    {
        var scope = new RegistrationTestDataScope(connectionString);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO meetings.venues
                    ("Id", "Name", "Address", "City", "Capacity")
                VALUES
                    (@venueId, @venueName, @address, @city, @capacity);

                INSERT INTO meetings.meetings
                    ("Id", "Title", "Description", "Status", "StartsAt", "EndsAt", "CreatedAt", "VenueId")
                VALUES
                    (@meetingId, @meetingTitle, @description, @status, @startsAt, @endsAt, @createdAt, @venueId);

                INSERT INTO registrations.attendees
                    ("Id", "FullName", "Email")
                VALUES
                    (@attendeeId, @attendeeName, @attendeeEmail);
                """;

            var now = DateTimeOffset.UtcNow;
            command.Parameters.AddWithValue("venueId", scope.VenueId);
            command.Parameters.AddWithValue("venueName", $"System Test Venue {scope.VenueId:N}");
            command.Parameters.AddWithValue("address", "1 System Test Street");
            command.Parameters.AddWithValue("city", "Test City");
            command.Parameters.AddWithValue("capacity", 10);
            command.Parameters.AddWithValue("meetingId", scope.MeetingId);
            command.Parameters.AddWithValue("meetingTitle", scope.MeetingTitle);
            command.Parameters.AddWithValue(
                "description",
                "Owned by an isolated MeetingFlow system test.");
            command.Parameters.AddWithValue("status", "Published");
            command.Parameters.AddWithValue("startsAt", now.AddDays(30));
            command.Parameters.AddWithValue("endsAt", now.AddDays(30).AddHours(2));
            command.Parameters.AddWithValue("createdAt", now);
            command.Parameters.AddWithValue("attendeeId", scope.AttendeeId);
            command.Parameters.AddWithValue("attendeeName", "System Test Attendee");
            command.Parameters.AddWithValue(
                "attendeeEmail",
                $"system-test-{scope.AttendeeId:N}@meetingflow.test");

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return scope;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM notifications.notifications
            WHERE "AttendeeId" = @attendeeId;

            DELETE FROM feedback.feedback
            WHERE "AttendeeId" = @attendeeId OR "MeetingId" = @meetingId;

            DELETE FROM meetings.tasks
            WHERE "MeetingId" = @meetingId;

            DELETE FROM registrations.registrations
            WHERE "AttendeeId" = @attendeeId OR "MeetingId" = @meetingId;

            DELETE FROM registrations.attendees
            WHERE "Id" = @attendeeId;

            DELETE FROM meetings.sessions
            WHERE "MeetingId" = @meetingId;

            DELETE FROM meetings.meetings
            WHERE "Id" = @meetingId;

            DELETE FROM meetings.venues
            WHERE "Id" = @venueId;
            """;
        command.Parameters.AddWithValue("venueId", VenueId);
        command.Parameters.AddWithValue("meetingId", MeetingId);
        command.Parameters.AddWithValue("attendeeId", AttendeeId);

        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static void EnsureLocalDatabase(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Host is not ("127.0.0.1" or "localhost" or "::1"))
        {
            throw new InvalidOperationException(
                "System-test data setup is restricted to a local PostgreSQL instance.");
        }
    }
}
