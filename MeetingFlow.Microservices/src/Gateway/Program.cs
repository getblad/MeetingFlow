using Gateway.Clients;
using Gateway.Contracts;
using Gateway.Mappings;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// In Docker, env vars like ServiceUrls__MeetingsManager automatically override appsettings.json.
var meetingsManagerUrl = builder.Configuration["ServiceUrls:MeetingsManager"] ?? "http://localhost:5030";
var registrationsManagerUrl = builder.Configuration["ServiceUrls:RegistrationsManager"] ?? "http://localhost:5031";
var aiChatEngineUrl = builder.Configuration["ServiceUrls:AiChatEngine"] ?? "http://localhost:5040";

builder.Services.AddHttpClient<MeetingsManagerClient>(c => c.BaseAddress = new Uri(meetingsManagerUrl));
builder.Services.AddHttpClient<RegistrationsManagerClient>(c => c.BaseAddress = new Uri(registrationsManagerUrl));
builder.Services.AddHttpClient<AiChatEngineClient>(c => c.BaseAddress = new Uri(aiChatEngineUrl));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Gateway" }));

app.MapPost("/venues", async (
    Gateway.Contracts.CreateVenueRequest request,
    MeetingsManagerClient client) =>
{
    var downstream = await client.CreateVenueAsync(
        new MeetingsManager.Contracts.CreateVenueRequest(
            request.Name,
            request.Address,
            request.City,
            request.Capacity));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Created($"/venues/{downstream.Value.Id}", downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.MapDelete("/venues/{id:guid}", async (Guid id, MeetingsManagerClient client) =>
{
    var downstream = await client.DeleteVenueAsync(id);
    return downstream.IsSuccess ? Results.NoContent() : ToErrorStatus(downstream);
});

app.MapGet("/meetings", async (MeetingsManagerClient client) =>
    Results.Ok((await client.GetAllAsync()).Select(meeting => meeting.ToPublicDto())));

app.MapGet("/meetings/{id:guid}", async (Guid id, MeetingsManagerClient client) =>
    await client.GetByIdAsync(id) is { } meeting
        ? Results.Ok(meeting.ToPublicDto())
        : Results.NotFound());

app.MapPost("/meetings", async (
    Gateway.Contracts.CreateMeetingRequest request,
    MeetingsManagerClient client) =>
{
    var downstream = await client.CreateMeetingAsync(
        new MeetingsManager.Contracts.CreateMeetingRequest(
            request.Title,
            request.Description,
            request.Status,
            request.StartsAt,
            request.EndsAt,
            request.VenueId));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Created(
            $"/meetings/{downstream.Value.Id}",
            downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.MapDelete("/meetings/{id:guid}", async (Guid id, MeetingsManagerClient client) =>
{
    var downstream = await client.DeleteMeetingAsync(id);
    return downstream.IsSuccess ? Results.NoContent() : ToErrorStatus(downstream);
});

app.MapPut("/meetings/{id:guid}", async (
    Guid id,
    Gateway.Contracts.UpdateMeetingRequest request,
    MeetingsManagerClient client) =>
{
    var downstream = await client.UpdateAsync(
        id,
        new MeetingsManager.Contracts.UpdateMeetingRequest(
            request.Title,
            request.Description,
            request.Status,
            request.StartsAt,
            request.EndsAt,
            request.VenueId));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Ok(downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.MapGet("/speakers", async (MeetingsManagerClient client) =>
    Results.Ok((await client.GetSpeakersAsync()).Select(speaker => speaker.ToPublicDto())));

app.MapGet("/speakers/{id:guid}", async (Guid id, MeetingsManagerClient client) =>
    await client.GetSpeakerByIdAsync(id) is { } speaker
        ? Results.Ok(speaker.ToPublicDto())
        : Results.NotFound());

app.MapPost("/attendees", async (
    Gateway.Contracts.CreateAttendeeRequest request,
    RegistrationsManagerClient client) =>
{
    var downstream = await client.CreateAttendeeAsync(
        new RegistrationsManager.Contracts.CreateAttendeeRequest(
            request.FullName,
            request.Email,
            request.Phone,
            request.Company));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Created(
            $"/attendees/{downstream.Value.Id}",
            downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.MapDelete("/attendees/{id:guid}", async (
    Guid id,
    RegistrationsManagerClient client) =>
{
    var downstream = await client.DeleteAttendeeAsync(id);
    return downstream.IsSuccess ? Results.NoContent() : ToErrorStatus(downstream);
});

app.MapPost("/registrations", async (
    Gateway.Contracts.CreateRegistrationRequest request,
    RegistrationsManagerClient client) =>
{
    var downstream = await client.CreateRegistrationAsync(
        new RegistrationsManager.Contracts.CreateRegistrationRequest(
            request.MeetingId,
            request.AttendeeId,
            request.TicketType));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Created(
            $"/registrations/{downstream.Value.Registration.Id}",
            downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.MapGet("/registrations/by-meeting/{meetingId:guid}", async (Guid meetingId, RegistrationsManagerClient client) =>
    Results.Ok((await client.GetRegistrationsByMeetingAsync(meetingId))
        .Select(registration => registration.ToPublicDto())));

app.MapPost("/feedback", async (
    Gateway.Contracts.SubmitFeedbackRequest request,
    RegistrationsManagerClient client) =>
{
    var downstream = await client.CreateFeedbackAsync(
        new RegistrationsManager.Contracts.SubmitFeedbackRequest(
            request.MeetingId,
            request.AttendeeId,
            request.Rating,
            request.Comment));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Created(
            $"/feedback/{downstream.Value.Id}",
            downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.MapPost("/chat", async (
    Gateway.Contracts.ChatRequest request,
    AiChatEngineClient client) =>
{
    var downstream = await client.ChatAsync(
        new AiChatEngine.Contracts.ChatRequest(
            request.Message,
            request.History?
                .Select(message => new AiChatEngine.Contracts.ChatMessageDto(
                    message.Role,
                    message.Content))
                .ToList()));

    return downstream.IsSuccess && downstream.Value is not null
        ? Results.Ok(downstream.Value.ToPublicDto())
        : ToErrorResult(downstream);
});

app.Run();

static IResult ToErrorResult<T>(DownstreamResult<T> downstream) =>
    downstream.Error is { } error
        ? Results.Json(error, statusCode: (int)downstream.StatusCode)
        : Results.StatusCode((int)downstream.StatusCode);

static IResult ToErrorStatus(DownstreamStatus downstream) =>
    downstream.Error is { } error
        ? Results.Json(error, statusCode: (int)downstream.StatusCode)
        : Results.StatusCode((int)downstream.StatusCode);
