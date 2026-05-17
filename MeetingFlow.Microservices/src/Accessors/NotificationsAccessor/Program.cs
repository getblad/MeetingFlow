using Microsoft.EntityFrameworkCore;
using NotificationsAccessor.Data;
using NotificationsAccessor.Infrastructure;
using NotificationsAccessor.Models;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["POSTGRES_CONN"]
           ?? Environment.GetEnvironmentVariable("POSTGRES_CONN")
           ?? "Host=localhost;Port=5432;Database=meetingflow;Username=meetingflow;Password=meetingflow";

builder.Services.AddDbContext<NotificationsDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddSingleton<FakeSmtpGateway>();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    for (var i = 0; i < 20; i++)
    {
        try { db.Database.EnsureCreated(); break; }
        catch { Thread.Sleep(1500); }
    }
    SeedData.Initialize(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "NotificationsAccessor" }));

app.MapGet("/notifications", async (NotificationsDbContext db) =>
    Results.Ok(await db.Notifications.OrderByDescending(n => n.SentAt).ToListAsync()));

app.MapGet("/notifications/by-attendee/{attendeeId:guid}", async (Guid attendeeId, NotificationsDbContext db) =>
    Results.Ok(await db.Notifications.Where(n => n.AttendeeId == attendeeId).ToListAsync()));

app.MapPost("/notifications", async (Notification body, NotificationsDbContext db) =>
{
    db.Notifications.Add(body);
    await db.SaveChangesAsync();
    return Results.Created($"/notifications/{body.Id}", body);
});

// Intentionally over-fetched: receives the entire Attendee + entire Meeting just to send an email.
app.MapPost("/notifications/send", async (SendNotificationCommand cmd, NotificationsDbContext db, FakeSmtpGateway smtp) =>
{
    var notification = new Notification
    {
        Id = Guid.NewGuid(),
        AttendeeId = cmd.Attendee.Id,
        Type = cmd.Channel,
        Subject = $"{cmd.Channel}: {cmd.Meeting.Title}",
        Body = cmd.Body,
        RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(cmd),
        SentAt = DateTimeOffset.UtcNow
    };
    db.Notifications.Add(notification);
    await db.SaveChangesAsync();
    await smtp.SendAsync(cmd.Attendee.Email, notification.Subject, cmd.Body, notification.RawPayloadJson);
    return Results.Ok(notification);
});

app.Run();

// Bound from the request body; deliberately accepts whole entities.
public record SendNotificationCommand(
    NotificationsAccessor.Models.Attendee Attendee,
    NotificationsAccessor.Models.Meeting Meeting,
    string Channel,
    string Body);
