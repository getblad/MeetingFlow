using SchedulingEngine.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SchedulingEngine.Api" }));

// Pure logic. Receives the candidate session AND the list of already-scheduled sessions
// in the room. Each one is sent as a full Session entity even though only time/room is used.
app.MapPost("/scheduling/check-conflict", (ConflictCheckCommand cmd) =>
{
    var hasConflict = cmd.Existing.Any(s =>
        s.Id != cmd.Candidate.Id &&
        s.RoomName.Equals(cmd.Candidate.RoomName, StringComparison.OrdinalIgnoreCase) &&
        s.StartsAt < cmd.Candidate.EndsAt &&
        s.EndsAt > cmd.Candidate.StartsAt);
    return Results.Ok(new { conflict = hasConflict });
});

// Capacity check. Manager sends the full Meeting entity plus venue capacity plus
// the current registration count even though only the last two numbers are needed.
app.MapPost("/scheduling/check-capacity", (CapacityCheckCommand cmd) =>
{
    var available = cmd.VenueCapacity - cmd.CurrentRegistrationCount;
    return Results.Ok(new
    {
        hasCapacity = available > 0,
        available,
        meetingTitle = cmd.Meeting.Title
    });
});

app.Run();

public record ConflictCheckCommand(Session Candidate, List<Session> Existing);
public record CapacityCheckCommand(Meeting Meeting, int VenueCapacity, int CurrentRegistrationCount);
