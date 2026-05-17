using RegistrationsManager.Clients;
using RegistrationsManager.Models;
using RegistrationsManager.Pricing;

var builder = WebApplication.CreateBuilder(args);

var dataAccessorUrl = builder.Configuration["DATA_ACCESSOR_URL"]
    ?? Environment.GetEnvironmentVariable("DATA_ACCESSOR_URL")
    ?? "http://localhost:5010";
var schedulingEngineUrl = builder.Configuration["SCHEDULING_ENGINE_URL"]
    ?? Environment.GetEnvironmentVariable("SCHEDULING_ENGINE_URL")
    ?? "http://localhost:5020";
var notificationsAccessorUrl = builder.Configuration["NOTIFICATIONS_ACCESSOR_URL"]
    ?? Environment.GetEnvironmentVariable("NOTIFICATIONS_ACCESSOR_URL")
    ?? "http://localhost:5011";

builder.Services.AddHttpClient<DataAccessorClient>(c => c.BaseAddress = new Uri(dataAccessorUrl));
builder.Services.AddHttpClient<SchedulingEngineClient>(c => c.BaseAddress = new Uri(schedulingEngineUrl));
builder.Services.AddHttpClient<NotificationsAccessorClient>(c => c.BaseAddress = new Uri(notificationsAccessorUrl));

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "RegistrationsManager" }));

app.MapGet("/registrations/by-meeting/{meetingId:guid}", async (Guid meetingId, DataAccessorClient data) =>
    Results.Ok(await data.GetRegistrationsForMeetingAsync(meetingId)));

// Accepts the FULL Registration entity from the caller — including PaymentStatus
// and InternalPaymentReference which the client should never control.
app.MapPost("/registrations", async (
    Registration body,
    DataAccessorClient data,
    SchedulingEngineClient scheduling,
    NotificationsAccessorClient notifications) =>
{
    var meeting = await data.GetMeetingAsync(body.MeetingId);
    if (meeting is null) return Results.NotFound(new { error = "Meeting not found" });

    var attendee = await data.GetAttendeeAsync(body.AttendeeId);
    if (attendee is null) return Results.NotFound(new { error = "Attendee not found" });

    var capacity = meeting.Venue?.Capacity ?? 0;
    var current = (await data.GetRegistrationsForMeetingAsync(body.MeetingId)).Count;
    var hasCapacity = await scheduling.HasCapacityAsync(meeting, capacity, current);
    if (!hasCapacity) return Results.Conflict(new { error = "Meeting is at capacity" });

    var price = InlineTicketPricing.CalculatePrice(meeting, body);

    if (body.Id == Guid.Empty) body.Id = Guid.NewGuid();
    if (body.RegisteredAt == default) body.RegisteredAt = DateTimeOffset.UtcNow;

    var saved = await data.CreateRegistrationAsync(body);

    await notifications.SendRegistrationConfirmationAsync(attendee, meeting);

    return Results.Created($"/registrations/{saved!.Id}", new
    {
        registration = saved,
        calculatedPrice = price
    });
});

// Accepts the FULL Feedback entity — including ModerationNotes a client can set.
app.MapPost("/feedback", async (Feedback body, DataAccessorClient data) =>
{
    if (body.Id == Guid.Empty) body.Id = Guid.NewGuid();
    if (body.CreatedAt == default) body.CreatedAt = DateTimeOffset.UtcNow;
    var saved = await data.CreateFeedbackAsync(body);
    return Results.Created($"/feedback/{saved!.Id}", saved);
});

app.Run();
