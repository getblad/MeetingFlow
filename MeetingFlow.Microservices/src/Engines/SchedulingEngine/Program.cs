using SchedulingEngine.Contracts;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SchedulingEngine" }));

app.MapPost("/scheduling/check-conflict", (CheckConflictRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Candidate.RoomName)
        || request.Candidate.StartsAt >= request.Candidate.EndsAt)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["candidate"] = ["RoomName is required and StartsAt must be before EndsAt."]
        });
    }

    var hasConflict = request.Existing.Any(session =>
        session.Id != request.Candidate.Id
        && session.RoomName.Equals(
            request.Candidate.RoomName,
            StringComparison.OrdinalIgnoreCase)
        && session.StartsAt < request.Candidate.EndsAt
        && session.EndsAt > request.Candidate.StartsAt);

    return Results.Ok(new CheckConflictResult(hasConflict));
});

app.MapPost("/scheduling/check-capacity", (CheckCapacityRequest request) =>
{
    if (request.VenueCapacity < 0 || request.CurrentRegistrationCount < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["capacity"] = ["Capacity and registration count cannot be negative."]
        });
    }

    var available = Math.Max(
        0,
        request.VenueCapacity - request.CurrentRegistrationCount);

    return Results.Ok(new CheckCapacityResult(available > 0, available));
});

app.Run();

// WebApplicationFactory uses this entry point to start the complete HTTP
// component in the test process.
public partial class Program { }
