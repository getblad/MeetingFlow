using DataAccessor.Data;
using DataAccessor.Contracts;
using DataAccessor.Mappings;
using DataAccessor.Models;
using DataAccessor.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["POSTGRES_CONN"]
           ?? Environment.GetEnvironmentVariable("POSTGRES_CONN")
           ?? "Host=localhost;Port=5432;Database=meetingflow;Username=meetingflow;Password=meetingflow";

builder.Services.AddDbContext<MeetingFlowDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddScoped<MeetingsRepository>();
builder.Services.AddScoped<RegistrationsRepository>();
builder.Services.AddScoped<FeedbackRepository>();
builder.Services.AddScoped<MeetingTasksRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MeetingFlowDbContext>();
    for (var i = 0; i < 20; i++)
    {
        try
        {
            db.Database.EnsureCreated();
            var creator = db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
            try { creator.CreateTables(); } catch { /* Tables may already exist */ }
            break;
        }
        catch { Thread.Sleep(1500); }
    }
    SeedData.Initialize(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "DataAccessor" }));

// Only transport DTOs cross the accessor boundary. EF entities stay internal.
app.MapGet("/data/meetings", async (MeetingsRepository r) =>
    Results.Ok((await r.GetAllAsync()).Select(m => m.ToSummaryDto())));

app.MapGet("/data/meetings/{id:guid}", async (Guid id, MeetingsRepository r) =>
    await r.GetByIdAsync(id) is { } meeting
        ? Results.Ok(meeting.ToDetailsDto())
        : Results.NotFound());

app.MapGet("/data/admin/meetings", async (MeetingsRepository r) =>
    Results.Ok((await r.GetAllAsync()).Select(m => m.ToAdminDto())));

app.MapPost("/data/venues", async (CreateVenueRequest body, MeetingsRepository r) =>
{
    if (string.IsNullOrWhiteSpace(body.Name)
        || string.IsNullOrWhiteSpace(body.Address)
        || string.IsNullOrWhiteSpace(body.City)
        || body.Capacity <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["venue"] = ["Name, address, city and a positive capacity are required."]
        });
    }

    var venue = await r.CreateVenueAsync(new Venue
    {
        Id = Guid.NewGuid(),
        Name = body.Name.Trim(),
        Address = body.Address.Trim(),
        City = body.City.Trim(),
        Capacity = body.Capacity
    });
    return Results.Created($"/data/venues/{venue.Id}", venue.ToDto());
});

app.MapDelete("/data/venues/{id:guid}", async (Guid id, MeetingsRepository r) =>
    ToDeleteResult(
        await r.DeleteVenueAsync(id),
        "Venue is used by one or more meetings."));

app.MapPost("/data/meetings", async (CreateMeetingRequest body, MeetingsRepository r) =>
{
    if (string.IsNullOrWhiteSpace(body.Title)
        || string.IsNullOrWhiteSpace(body.Status)
        || body.VenueId == Guid.Empty
        || body.StartsAt >= body.EndsAt)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["meeting"] =
            ["Title, status, venue and a valid time range are required."]
        });
    }

    var meeting = await r.CreateMeetingAsync(new Meeting
    {
        Id = Guid.NewGuid(),
        Title = body.Title.Trim(),
        Description = body.Description,
        Status = body.Status.Trim(),
        StartsAt = body.StartsAt,
        EndsAt = body.EndsAt,
        CreatedAt = DateTimeOffset.UtcNow,
        VenueId = body.VenueId
    });

    return meeting is null
        ? Results.NotFound(new { error = "Venue not found" })
        : Results.Created($"/data/meetings/{meeting.Id}", meeting.ToDetailsDto());
});

app.MapDelete("/data/meetings/{id:guid}", async (Guid id, MeetingsRepository r) =>
    ToDeleteResult(
        await r.DeleteMeetingAsync(id),
        "Meeting has related sessions, registrations, feedback or tasks."));

app.MapGet("/data/meetings/{id:guid}/registration-context", async (
    Guid id,
    MeetingsRepository r) =>
    await r.GetRegistrationContextAsync(id) is { } meeting
        ? Results.Ok(meeting.ToRegistrationContextDto())
        : Results.NotFound());

app.MapPut("/data/meetings/{id:guid}", async (
    Guid id,
    UpdateMeetingRequest body,
    MeetingsRepository r) =>
{
    if (string.IsNullOrWhiteSpace(body.Title) || body.StartsAt >= body.EndsAt)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["meeting"] = ["Title is required and StartsAt must be before EndsAt."]
        });
    }

    var saved = await r.UpdateAsync(
        id,
        body.Title.Trim(),
        body.Description,
        body.Status,
        body.StartsAt,
        body.EndsAt,
        body.VenueId);

    return saved is null ? Results.NotFound() : Results.Ok(saved.ToDetailsDto());
});

app.MapGet("/data/meetings/{id:guid}/sessions", async (Guid id, MeetingsRepository r) =>
    Results.Ok((await r.GetSessionsByMeetingAsync(id)).Select(s => s.ToDto())));

// Speakers
app.MapGet("/data/speakers", async (MeetingsRepository r) =>
    Results.Ok((await r.GetAllSpeakersAsync()).Select(s => s.ToDto())));
app.MapGet("/data/speakers/{id:guid}", async (Guid id, MeetingsRepository r) =>
    await r.GetSpeakerByIdAsync(id) is { } speaker
        ? Results.Ok(speaker.ToDto())
        : Results.NotFound());

// Registrations repository endpoints.
app.MapGet("/data/registrations", async (RegistrationsRepository r) =>
    Results.Ok((await r.GetAllAsync()).Select(registration => registration.ToDto())));
app.MapGet("/data/registrations/by-meeting/{meetingId:guid}", async (Guid meetingId, RegistrationsRepository r) =>
    Results.Ok((await r.GetByMeetingAsync(meetingId)).Select(registration => registration.ToDto())));
app.MapPost("/data/registrations", async (
    PersistRegistrationRequest body,
    RegistrationsRepository r) =>
{
    var registration = new Registration
    {
        Id = Guid.NewGuid(),
        MeetingId = body.MeetingId,
        AttendeeId = body.AttendeeId,
        RegisteredAt = DateTimeOffset.UtcNow,
        TicketType = body.TicketType,
        PaymentStatus = "Pending"
    };
    var saved = await r.CreateAsync(registration);
    return Results.Created($"/data/registrations/{saved.Id}", saved.ToDto());
});

// Test support is opt-in and is intentionally not routed through Gateway.
// It is absent from the application's endpoint table unless explicitly enabled.
if (app.Configuration.GetValue<bool>("TestSupport:Enabled"))
{
    app.MapDelete("/_test/registrations/by-attendee/{attendeeId:guid}", async (
        Guid attendeeId,
        RegistrationsRepository r) =>
    {
        await r.DeleteRegistrationsByAttendeeAsync(attendeeId);
        return Results.NoContent();
    });
}

app.MapGet("/data/attendees/{id:guid}/contact", async (Guid id, RegistrationsRepository r) =>
    await r.GetAttendeeAsync(id) is { } attendee
        ? Results.Ok(attendee.ToContactDto())
        : Results.NotFound());

app.MapPost("/data/attendees", async (CreateAttendeeRequest body, RegistrationsRepository r) =>
{
    if (string.IsNullOrWhiteSpace(body.FullName)
        || string.IsNullOrWhiteSpace(body.Email)
        || !body.Email.Contains('@', StringComparison.Ordinal))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["attendee"] = ["FullName and a valid email are required."]
        });
    }

    var attendee = await r.CreateAttendeeAsync(new Attendee
    {
        Id = Guid.NewGuid(),
        FullName = body.FullName.Trim(),
        Email = body.Email.Trim(),
        Phone = body.Phone,
        Company = body.Company
    });
    return Results.Created(
        $"/data/attendees/{attendee.Id}",
        attendee.ToDetailsDto());
});

app.MapDelete("/data/attendees/{id:guid}", async (Guid id, RegistrationsRepository r) =>
    ToDeleteResult(
        await r.DeleteAttendeeAsync(id),
        "Attendee has related registrations or feedback."));

// Feedback repository endpoints.
app.MapGet("/data/feedback/by-meeting/{meetingId:guid}", async (Guid meetingId, FeedbackRepository r) =>
    Results.Ok((await r.GetByMeetingAsync(meetingId)).Select(feedback => feedback.ToDto())));
app.MapPost("/data/feedback", async (PersistFeedbackRequest body, FeedbackRepository r) =>
{
    var feedback = new Feedback
    {
        Id = Guid.NewGuid(),
        MeetingId = body.MeetingId,
        AttendeeId = body.AttendeeId,
        Rating = body.Rating,
        Comment = body.Comment,
        CreatedAt = DateTimeOffset.UtcNow
    };
    var saved = await r.CreateAsync(feedback);
    return Results.Created($"/data/feedback/{saved.Id}", saved.ToDto());
});

// --- Meeting Tasks (action items) ---
app.MapGet("/data/tasks", async (MeetingTasksRepository r) =>
    Results.Ok((await r.GetAllAsync()).Select(task => task.ToDto())));
app.MapGet("/data/tasks/by-meeting/{meetingId:guid}", async (Guid meetingId, MeetingTasksRepository r) =>
    Results.Ok((await r.GetByMeetingAsync(meetingId)).Select(task => task.ToDto())));
app.MapGet("/data/tasks/{id:guid}", async (Guid id, MeetingTasksRepository r) =>
    await r.GetByIdAsync(id) is { } task ? Results.Ok(task.ToDto()) : Results.NotFound());
app.MapPost("/data/tasks", async (CreateMeetingTaskRequest body, MeetingTasksRepository r) =>
{
    var task = new MeetingTask
    {
        MeetingId = body.MeetingId,
        Title = body.Title,
        AssignedTo = body.AssignedTo
    };
    var saved = await r.CreateAsync(task);
    return Results.Created($"/data/tasks/{saved.Id}", saved.ToDto());
});
app.MapPut("/data/tasks/{id:guid}", async (
    Guid id,
    UpdateMeetingTaskRequest body,
    MeetingTasksRepository r) =>
{
    var update = new MeetingTask
    {
        Title = body.Title,
        IsCompleted = body.IsCompleted,
        AssignedTo = body.AssignedTo
    };
    return await r.UpdateAsync(id, update) is { } task
        ? Results.Ok(task.ToDto())
        : Results.NotFound();
});
app.MapDelete("/data/tasks/{id:guid}", async (Guid id, MeetingTasksRepository r) =>
    await r.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

app.Run();

static IResult ToDeleteResult(DeleteResult result, string conflictMessage) =>
    result switch
    {
        DeleteResult.Deleted => Results.NoContent(),
        DeleteResult.NotFound => Results.NotFound(),
        DeleteResult.HasDependencies => Results.Conflict(new { error = conflictMessage }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };

// WebApplicationFactory uses this entry point to start the complete HTTP
// component in the test process.
public partial class Program { }
