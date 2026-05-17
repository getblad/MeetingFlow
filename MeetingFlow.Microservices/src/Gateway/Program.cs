using Gateway.Clients;

var builder = WebApplication.CreateBuilder(args);

var meetingsManagerUrl = builder.Configuration["MEETINGS_MANAGER_URL"]
    ?? Environment.GetEnvironmentVariable("MEETINGS_MANAGER_URL")
    ?? "http://localhost:5030";
var registrationsManagerUrl = builder.Configuration["REGISTRATIONS_MANAGER_URL"]
    ?? Environment.GetEnvironmentVariable("REGISTRATIONS_MANAGER_URL")
    ?? "http://localhost:5031";

builder.Services.AddHttpClient<MeetingsManagerClient>(c => c.BaseAddress = new Uri(meetingsManagerUrl));
builder.Services.AddHttpClient<RegistrationsManagerClient>(c => c.BaseAddress = new Uri(registrationsManagerUrl));

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Gateway" }));

// --- Meetings ---
// Returns whatever the manager returns. No public/edge model.
app.MapGet("/meetings", async (MeetingsManagerClient client) => Results.Ok(await client.GetAllAsync()));

app.MapGet("/meetings/{id:guid}", async (Guid id, MeetingsManagerClient client) =>
{
    var resp = await client.GetByIdRawAsync(id);
    var content = await resp.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", statusCode: (int)resp.StatusCode);
});

// Public can hit admin endpoint with no auth. By design — students should add the boundary.
app.MapGet("/admin/meetings", async (MeetingsManagerClient client) =>
    Results.Ok(await client.GetAdminListAsync()));

app.MapPut("/meetings/{id:guid}", async (Guid id, HttpRequest req, MeetingsManagerClient client) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
    var resp = await client.UpdateRawAsync(id, content);
    var responseBody = await resp.Content.ReadAsStringAsync();
    return Results.Content(responseBody, "application/json", statusCode: (int)resp.StatusCode);
});

// --- Speakers ---
app.MapGet("/speakers", async (MeetingsManagerClient client) =>
{
    var resp = await client.GetSpeakersRawAsync();
    var content = await resp.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", statusCode: (int)resp.StatusCode);
});

app.MapGet("/speakers/{id:guid}", async (Guid id, MeetingsManagerClient client) =>
{
    var resp = await client.GetSpeakerByIdRawAsync(id);
    var content = await resp.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", statusCode: (int)resp.StatusCode);
});

// --- Registrations ---
app.MapPost("/registrations", async (HttpRequest req, RegistrationsManagerClient client) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
    var resp = await client.CreateRegistrationRawAsync(content);
    var responseBody = await resp.Content.ReadAsStringAsync();
    return Results.Content(responseBody, "application/json", statusCode: (int)resp.StatusCode);
});

app.MapGet("/registrations/by-meeting/{meetingId:guid}", async (Guid meetingId, RegistrationsManagerClient client) =>
{
    var resp = await client.GetRegistrationsByMeetingRawAsync(meetingId);
    var content = await resp.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", statusCode: (int)resp.StatusCode);
});

app.MapPost("/feedback", async (HttpRequest req, RegistrationsManagerClient client) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
    var resp = await client.CreateFeedbackRawAsync(content);
    var responseBody = await resp.Content.ReadAsStringAsync();
    return Results.Content(responseBody, "application/json", statusCode: (int)resp.StatusCode);
});

app.Run();
