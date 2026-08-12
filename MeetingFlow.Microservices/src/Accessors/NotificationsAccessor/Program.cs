using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NotificationsAccessor.Contracts;
using NotificationsAccessor.Data;
using NotificationsAccessor.Infrastructure;
using NotificationsAccessor.Messaging;
using NotificationsAccessor.Models;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["POSTGRES_CONN"]
           ?? Environment.GetEnvironmentVariable("POSTGRES_CONN")
           ?? "Host=localhost;Port=5432;Database=meetingflow;Username=meetingflow;Password=meetingflow";

builder.Services.AddDbContext<NotificationsDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddSingleton<FakeSmtpGateway>();
builder.Services.AddHostedService<RegistrationEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    for (var i = 0; i < 20; i++)
    {
        try
        {
            // EnsureCreated() won't create tables if OTHER tables already exist in the DB.
            // Use GetService to explicitly create this context's tables.
            db.Database.EnsureCreated();
            var creator = db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
            try { creator.CreateTables(); } catch { /* Tables may already exist */ }
            break;
        }
        catch { Thread.Sleep(1500); }
    }
    SeedData.Initialize(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "NotificationsAccessor" }));

app.MapGet("/notifications", async (NotificationsDbContext db) =>
    Results.Ok((await db.Notifications
            .OrderByDescending(notification => notification.SentAt)
            .ToListAsync())
        .Select(ToDto)));

app.MapGet("/notifications/by-attendee/{attendeeId:guid}", async (Guid attendeeId, NotificationsDbContext db) =>
    Results.Ok((await db.Notifications
            .Where(notification => notification.AttendeeId == attendeeId)
            .OrderByDescending(notification => notification.SentAt)
            .ToListAsync())
        .Select(ToDto)));

app.MapPost("/notifications/send", async (
    SendNotificationRequest request,
    NotificationsDbContext db,
    FakeSmtpGateway smtp) =>
{
    if (string.IsNullOrWhiteSpace(request.RecipientEmail)
        || string.IsNullOrWhiteSpace(request.Subject)
        || string.IsNullOrWhiteSpace(request.Body))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["notification"] = ["RecipientEmail, Subject and Body are required."]
        });
    }

    var notification = new Notification
    {
        Id = Guid.NewGuid(),
        AttendeeId = request.AttendeeId,
        Type = request.Channel,
        Subject = request.Subject,
        Body = request.Body,
        RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(request),
        SentAt = DateTimeOffset.UtcNow
    };
    db.Notifications.Add(notification);
    await db.SaveChangesAsync();
    await smtp.SendAsync(
        request.RecipientEmail,
        notification.Subject,
        notification.Body,
        notification.RawPayloadJson);

    return Results.Ok(ToDto(notification));
});

// Test support is opt-in and is intentionally not routed through Gateway.
// Deleting by attendee makes cleanup possible even if a test fails before it
// captures the asynchronously created notification ID.
if (app.Configuration.GetValue<bool>("TestSupport:Enabled"))
{
    app.MapDelete("/_test/notifications/by-attendee/{attendeeId:guid}", async (
        Guid attendeeId,
        NotificationsDbContext db) =>
    {
        await db.Notifications
            .Where(notification => notification.AttendeeId == attendeeId)
            .ExecuteDeleteAsync();
        return Results.NoContent();
    });
}

app.Run();

static NotificationDto ToDto(Notification notification) =>
    new(
        notification.Id,
        notification.AttendeeId,
        notification.Type,
        notification.Subject,
        notification.Body,
        notification.SentAt);

// WebApplicationFactory uses this entry point to start the complete HTTP
// component and its hosted RabbitMQ consumer in the test process.
public partial class Program { }
