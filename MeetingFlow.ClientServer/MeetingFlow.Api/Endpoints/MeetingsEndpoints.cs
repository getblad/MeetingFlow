using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Data;

namespace MeetingFlow.Api.Endpoints;

public static class MeetingsEndpoints
{
    public static void MapMeetingsEndpoints(this WebApplication app)
    {
        // Educational baseline:
        // This endpoint returns EF Core entities directly.
        // In production, prefer a response DTO that exposes only fields needed by the client.
        app.MapGet("/api/meetings", async (MeetingFlowDbContext db) =>
        {
            var meetings = await db.Meetings
                .Include(e => e.Venue)
                .Include(e => e.Sessions)
                .OrderBy(e => e.StartsAt)
                .ToListAsync();

            return Results.Ok(meetings);
        });

        // Educational baseline:
        // This endpoint returns the full Meeting entity graph including internal fields,
        // all sessions, speakers, registrations, attendees, and feedback.
        // In production, prefer an MeetingDetailsResponse with only the fields needed.
        app.MapGet("/api/meetings/{id:guid}", async (Guid id, MeetingFlowDbContext db) =>
        {
            var ev = await db.Meetings
                .Include(e => e.Venue)
                .Include(e => e.Sessions).ThenInclude(s => s.Speaker)
                .Include(e => e.Registrations).ThenInclude(r => r.Attendee)
                .Include(e => e.Feedback).ThenInclude(f => f.Attendee)
                .FirstOrDefaultAsync(e => e.Id == id);

            return ev is null ? Results.NotFound() : Results.Ok(ev);
        });

        // Educational baseline:
        // This endpoint returns Meeting entities directly including InternalNotes and AdminOnlyCode.
        // In production, this should be behind authentication and return an AdminMeetingRowResponse.
        app.MapGet("/api/admin/meetings", async (MeetingFlowDbContext db) =>
        {
            var meetings = await db.Meetings
                .Include(e => e.Venue)
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Results.Ok(meetings);
        });

        // Educational baseline:
        // This endpoint accepts the full Meeting entity directly from the request body.
        // In production, prefer a dedicated UpdateMeetingRequest model.
        app.MapPut("/api/meetings/{id:guid}", async (Guid id, MeetingFlow.Api.Models.Meeting updated, MeetingFlowDbContext db) =>
        {
            var ev = await db.Meetings.FindAsync(id);
            if (ev is null) return Results.NotFound();

            ev.Title = updated.Title;
            ev.Description = updated.Description;
            ev.Status = updated.Status;
            ev.StartsAt = updated.StartsAt;
            ev.EndsAt = updated.EndsAt;
            ev.InternalNotes = updated.InternalNotes;
            ev.AdminOnlyCode = updated.AdminOnlyCode;
            ev.VenueId = updated.VenueId;
            ev.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ev);
        });
    }
}
