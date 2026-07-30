using MeetingsManager.Clients;
using MeetingsManager.Contracts;
using MeetingsManager.Mappings;
using SchedulingEngine.Contracts;

var builder = WebApplication.CreateBuilder(args);

var dataAccessorUrl = builder.Configuration["DATA_ACCESSOR_URL"]
    ?? Environment.GetEnvironmentVariable("DATA_ACCESSOR_URL")
    ?? "http://localhost:5010";
var schedulingEngineUrl = builder.Configuration["SCHEDULING_ENGINE_URL"]
    ?? Environment.GetEnvironmentVariable("SCHEDULING_ENGINE_URL")
    ?? "http://localhost:5020";

builder.Services.AddHttpClient<DataAccessorClient>(c => c.BaseAddress = new Uri(dataAccessorUrl));
builder.Services.AddHttpClient<SchedulingEngineClient>(c => c.BaseAddress = new Uri(schedulingEngineUrl));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MeetingsManager" }));

app.MapGet("/meetings", async (DataAccessorClient data) =>
    Results.Ok((await data.GetAllMeetingsAsync()).Select(meeting => meeting.ToManagerDto())));

app.MapGet("/meetings/{id:guid}", async (Guid id, DataAccessorClient data) =>
    await data.GetMeetingAsync(id) is { } meeting
        ? Results.Ok(meeting.ToManagerDto())
        : Results.NotFound());

app.MapGet("/admin/meetings", async (DataAccessorClient data) =>
    Results.Ok((await data.GetAdminMeetingsAsync()).Select(meeting => meeting.ToManagerDto())));

app.MapPut("/meetings/{id:guid}", async (
    Guid id,
    UpdateMeetingRequest body,
    DataAccessorClient data) =>
{
    if (string.IsNullOrWhiteSpace(body.Title) || body.StartsAt >= body.EndsAt)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["meeting"] = ["Title is required and StartsAt must be before EndsAt."]
        });
    }

    var request = new DataAccessor.Contracts.UpdateMeetingRequest(
        body.Title,
        body.Description,
        body.Status,
        body.StartsAt,
        body.EndsAt,
        body.VenueId);
    var saved = await data.UpdateMeetingAsync(id, request);
    return saved is null ? Results.NotFound() : Results.Ok(saved.ToManagerDto());
});

app.MapPost("/meetings/{meetingId:guid}/sessions/check", async (
    Guid meetingId,
    CheckSessionConflictRequest candidate,
    DataAccessorClient data,
    SchedulingEngineClient scheduling) =>
{
    if (string.IsNullOrWhiteSpace(candidate.RoomName)
        || candidate.StartsAt >= candidate.EndsAt)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["session"] = ["RoomName is required and StartsAt must be before EndsAt."]
        });
    }

    var existing = await data.GetSessionsForMeetingAsync(meetingId);
    var candidateSlot = new SessionSlotDto(
        candidate.SessionId,
        candidate.RoomName,
        candidate.StartsAt,
        candidate.EndsAt);
    var existingSlots = existing
        .Select(session => new SessionSlotDto(
            session.Id,
            session.RoomName,
            session.StartsAt,
            session.EndsAt))
        .ToList();
    var result = await scheduling.CheckConflictAsync(candidateSlot, existingSlots);

    return Results.Ok(new CheckSessionConflictResult(
        result.HasConflict,
        existing.Count));
});

app.MapGet("/speakers", async (DataAccessorClient data) =>
    Results.Ok((await data.GetSpeakersAsync()).Select(speaker => speaker.ToManagerDto())));
app.MapGet("/speakers/{id:guid}", async (Guid id, DataAccessorClient data) =>
    await data.GetSpeakerAsync(id) is { } speaker
        ? Results.Ok(speaker.ToManagerDto())
        : Results.NotFound());

app.Run();
