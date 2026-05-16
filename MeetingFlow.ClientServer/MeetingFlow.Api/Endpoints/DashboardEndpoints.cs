using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Data;

namespace MeetingFlow.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        // Educational baseline:
        // This endpoint returns an anonymous object instead of a named DTO.
        // This avoids creating a DTO class but is still not ideal — anonymous types
        // make API contracts implicit and hard to maintain.
        app.MapGet("/api/dashboard", async (MeetingFlowDbContext db) =>
        {
            var totalMeetings = await db.Meetings.CountAsync();
            var totalRegistrations = await db.Registrations.CountAsync();
            var totalSpeakers = await db.Speakers.CountAsync();
            var averageFeedbackRating = await db.Feedback.AnyAsync()
                ? await db.Feedback.AverageAsync(f => f.Rating)
                : 0.0;

            var allMeetings = await db.Meetings
                .Include(e => e.Venue)
                .Include(e => e.Registrations)
                .ToListAsync();
            var upcomingMeetings = allMeetings
                .Where(e => e.StartsAt > DateTimeOffset.UtcNow && e.Status == "Published")
                .OrderBy(e => e.StartsAt)
                .Take(5)
                .ToList();

            return Results.Ok(new
            {
                totalMeetings,
                totalRegistrations,
                totalSpeakers,
                averageFeedbackRating,
                upcomingMeetings
            });
        });
    }
}
