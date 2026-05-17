using NotificationsAccessor.Models;

namespace NotificationsAccessor.Data;

public static class SeedData
{
    public static void Initialize(NotificationsDbContext db)
    {
        if (db.Notifications.Any()) return;
        var now = DateTimeOffset.UtcNow;
        var attendee1 = Guid.Parse("e5000000-0000-0000-0000-000000000001");
        var attendee2 = Guid.Parse("e5000000-0000-0000-0000-000000000002");
        var attendee4 = Guid.Parse("e5000000-0000-0000-0000-000000000004");
        db.Notifications.AddRange(
            new Notification
            {
                Id = Guid.Parse("b8000000-0000-0000-0000-000000000001"),
                AttendeeId = attendee1, Type = "Email",
                Subject = "Registration Confirmed — Frontend Architecture Summit",
                Body = "Hi Alex, your registration is confirmed!",
                RawPayloadJson = "{\"template\":\"reg_confirm\",\"smtp_id\":\"msg_abc123\",\"provider\":\"SendGrid\",\"cost_cents\":0.12}",
                SentAt = now.AddDays(-10)
            },
            new Notification
            {
                Id = Guid.Parse("b8000000-0000-0000-0000-000000000002"),
                AttendeeId = attendee2, Type = "Email",
                Subject = "Registration Confirmed — Frontend Architecture Summit",
                Body = "Hi Priya, your registration is confirmed!",
                RawPayloadJson = "{\"template\":\"reg_confirm\",\"smtp_id\":\"msg_def456\",\"provider\":\"SendGrid\",\"cost_cents\":0.12}",
                SentAt = now.AddDays(-9)
            },
            new Notification
            {
                Id = Guid.Parse("b8000000-0000-0000-0000-000000000003"),
                AttendeeId = attendee4, Type = "SMS",
                Subject = "Meeting Reminder",
                Body = "Cloud Integration Day is in 7 days.",
                RawPayloadJson = "{\"provider\":\"Twilio\",\"sid\":\"SM_xyz789\",\"cost_cents\":1.50}",
                SentAt = now.AddDays(-5)
            });
        db.SaveChanges();
    }
}
