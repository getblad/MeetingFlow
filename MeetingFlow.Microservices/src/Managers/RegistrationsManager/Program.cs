using DataAccessor.Contracts;
using MeetingFlow.IntegrationEvents;
using RegistrationsManager.Clients;
using RegistrationsManager.Contracts;
using RegistrationsManager.Mappings;
using RegistrationsManager.Messaging;
using RegistrationsManager.Pricing;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var dataAccessorUrl = builder.Configuration["DATA_ACCESSOR_URL"]
    ?? Environment.GetEnvironmentVariable("DATA_ACCESSOR_URL")
    ?? "http://localhost:5010";
var schedulingEngineUrl = builder.Configuration["SCHEDULING_ENGINE_URL"]
    ?? Environment.GetEnvironmentVariable("SCHEDULING_ENGINE_URL")
    ?? "http://localhost:5020";
var rabbitUrl = builder.Configuration["RABBITMQ_URL"]
    ?? Environment.GetEnvironmentVariable("RABBITMQ_URL")
    ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddHttpClient<DataAccessorClient>(c => c.BaseAddress = new Uri(dataAccessorUrl));
builder.Services.AddHttpClient<SchedulingEngineClient>(c => c.BaseAddress = new Uri(schedulingEngineUrl));
builder.Services.AddSingleton<IEventPublisher>(
    _ => EventPublisher.CreateAsync(rabbitUrl).GetAwaiter().GetResult());
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "RegistrationsManager" }));

app.MapGet("/registrations/by-meeting/{meetingId:guid}", async (Guid meetingId, DataAccessorClient data) =>
    Results.Ok((await data.GetRegistrationsForMeetingAsync(meetingId))
        .Select(registration => registration.ToManagerDto())));

app.MapPost("/attendees", async (
    RegistrationsManager.Contracts.CreateAttendeeRequest request,
    DataAccessorClient data) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName)
        || string.IsNullOrWhiteSpace(request.Email)
        || !request.Email.Contains('@', StringComparison.Ordinal))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["attendee"] = ["FullName and a valid email are required."]
        });
    }

    var downstream = await data.CreateAttendeeAsync(
        new DataAccessor.Contracts.CreateAttendeeRequest(
            request.FullName,
            request.Email,
            request.Phone,
            request.Company));

    return downstream.StatusCode == HttpStatusCode.Created && downstream.Value is not null
        ? Results.Created(
            $"/attendees/{downstream.Value.Id}",
            downstream.Value.ToManagerDto())
        : Results.StatusCode((int)downstream.StatusCode);
});

app.MapDelete("/attendees/{id:guid}", async (Guid id, DataAccessorClient data) =>
{
    var statusCode = await data.DeleteAttendeeAsync(id);
    return statusCode switch
    {
        HttpStatusCode.NoContent => Results.NoContent(),
        HttpStatusCode.NotFound => Results.NotFound(),
        HttpStatusCode.Conflict => Results.Conflict(new
        {
            error = "Attendee has related registrations or feedback."
        }),
        _ => Results.StatusCode((int)statusCode)
    };
});

app.MapPost("/registrations", async (
    CreateRegistrationRequest request,
    DataAccessorClient data,
    SchedulingEngineClient scheduling,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider) =>
{
    var allowedTicketTypes = new[] { "VIP", "Early Bird", "Student", "General" };
    if (request.MeetingId == Guid.Empty
        || request.AttendeeId == Guid.Empty
        || !allowedTicketTypes.Contains(request.TicketType, StringComparer.OrdinalIgnoreCase))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["registration"] =
            [
                "MeetingId, AttendeeId and a supported TicketType are required."
            ]
        });
    }

    var meeting = await data.GetRegistrationContextAsync(request.MeetingId);
    if (meeting is null) return Results.NotFound(new { error = "Meeting not found" });

    var attendee = await data.GetAttendeeContactAsync(request.AttendeeId);
    if (attendee is null) return Results.NotFound(new { error = "Attendee not found" });

    var existing = await data.GetRegistrationsForMeetingAsync(request.MeetingId);
    if (existing.Any(registration => registration.AttendeeId == request.AttendeeId))
    {
        return Results.Conflict(new { error = "Attendee is already registered" });
    }

    var capacity = await scheduling.CheckCapacityAsync(
        meeting.VenueCapacity,
        existing.Count);
    if (!capacity.HasCapacity)
    {
        return Results.Conflict(new { error = "Meeting is at capacity" });
    }

    var normalizedTicketType = allowedTicketTypes.First(type =>
        type.Equals(request.TicketType, StringComparison.OrdinalIgnoreCase));
    var price = InlineTicketPricing.CalculatePrice(
        meeting,
        normalizedTicketType,
        timeProvider.GetUtcNow());

    var saved = await data.CreateRegistrationAsync(new PersistRegistrationRequest(
        request.MeetingId,
        request.AttendeeId,
        normalizedTicketType));

    await eventPublisher.PublishAsync(
        "registration.created.v1",
        new RegistrationCreatedV1(
            Guid.NewGuid(),
            saved.Id,
            meeting.Id,
            attendee.Id,
            meeting.Title,
            attendee.FullName,
            attendee.Email,
            saved.RegisteredAt));

    return Results.Created(
        $"/registrations/{saved.Id}",
        new CreateRegistrationResult(saved.ToManagerDto(), price));
});

app.MapPost("/feedback", async (SubmitFeedbackRequest request, DataAccessorClient data) =>
{
    if (request.MeetingId == Guid.Empty
        || request.AttendeeId == Guid.Empty
        || request.Rating is < 1 or > 5
        || string.IsNullOrWhiteSpace(request.Comment))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["feedback"] =
            [
                "MeetingId, AttendeeId, a rating from 1 to 5 and a comment are required."
            ]
        });
    }

    if (await data.GetRegistrationContextAsync(request.MeetingId) is null)
        return Results.NotFound(new { error = "Meeting not found" });
    if (await data.GetAttendeeContactAsync(request.AttendeeId) is null)
        return Results.NotFound(new { error = "Attendee not found" });

    var saved = await data.CreateFeedbackAsync(new PersistFeedbackRequest(
        request.MeetingId,
        request.AttendeeId,
        request.Rating,
        request.Comment.Trim()));

    return Results.Created($"/feedback/{saved.Id}", saved.ToManagerDto());
});

app.Run();

// WebApplicationFactory uses this entry point to start the complete HTTP
// component in the test process.
public partial class Program { }
