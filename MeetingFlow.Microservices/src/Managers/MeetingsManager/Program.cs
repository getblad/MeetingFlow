using MeetingsManager.Clients;
using MeetingsManager.Models;

var builder = WebApplication.CreateBuilder(args);

var dataAccessorUrl = builder.Configuration["DATA_ACCESSOR_URL"]
    ?? Environment.GetEnvironmentVariable("DATA_ACCESSOR_URL")
    ?? "http://localhost:5010";
var schedulingEngineUrl = builder.Configuration["SCHEDULING_ENGINE_URL"]
    ?? Environment.GetEnvironmentVariable("SCHEDULING_ENGINE_URL")
    ?? "http://localhost:5020";

builder.Services.AddHttpClient<DataAccessorClient>(c => c.BaseAddress = new Uri(dataAccessorUrl));
builder.Services.AddHttpClient<SchedulingEngineClient>(c => c.BaseAddress = new Uri(schedulingEngineUrl));

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MeetingsManager" }));

// Public meeting list — returns whatever the accessor returned, untouched.
app.MapGet("/meetings", async (DataAccessorClient data) =>
    Results.Ok(await data.GetAllMeetingsAsync()));

// Meeting details — returns the full entity graph including registrations and feedback.
app.MapGet("/meetings/{id:guid}", async (Guid id, DataAccessorClient data) =>
    await data.GetMeetingAsync(id) is { } m ? Results.Ok(m) : Results.NotFound());

// Admin meeting list — exact same payload as /meetings; intentionally no admin/public split.
app.MapGet("/admin/meetings", async (DataAccessorClient data) =>
    Results.Ok(await data.GetAllMeetingsAsync()));

// Update meeting — accepts the full Meeting entity from the caller.
app.MapPut("/meetings/{id:guid}", async (Guid id, Meeting body, DataAccessorClient data) =>
{
    var saved = await data.UpdateMeetingAsync(id, body);
    return saved is null ? Results.NotFound() : Results.Ok(saved);
});

// Add session — calls the SchedulingEngine with the FULL session entity for conflict check.
app.MapPost("/meetings/{meetingId:guid}/sessions/check", async (
    Guid meetingId, Session candidate,
    DataAccessorClient data, SchedulingEngineClient scheduling) =>
{
    var existing = await data.GetSessionsForMeetingAsync(meetingId);
    var conflict = await scheduling.HasConflictAsync(candidate, existing);
    return Results.Ok(new { conflict, existingCount = existing.Count });
});

// Speakers — return full Speaker entity including Email, Phone, InternalNotes.
app.MapGet("/speakers", async (DataAccessorClient data) => Results.Ok(await data.GetSpeakersAsync()));
app.MapGet("/speakers/{id:guid}", async (Guid id, DataAccessorClient data) =>
    await data.GetSpeakerAsync(id) is { } s ? Results.Ok(s) : Results.NotFound());

app.Run();
