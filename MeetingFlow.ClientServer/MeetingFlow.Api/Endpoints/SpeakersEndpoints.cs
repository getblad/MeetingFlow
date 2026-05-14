using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Data;

namespace MeetingFlow.Api.Endpoints;

public static class SpeakersEndpoints
{
    public static void MapSpeakersEndpoints(this WebApplication app)
    {
        // Educational baseline:
        // This endpoint returns the full Speaker entity including Email, Phone, InternalNotes.
        // In production, prefer a SpeakerProfileResponse that excludes sensitive fields.
        app.MapGet("/api/speakers/{id:guid}", async (Guid id, MeetingFlowDbContext db) =>
        {
            var speaker = await db.Speakers
                .Include(s => s.Sessions).ThenInclude(s => s.Meeting)
                .FirstOrDefaultAsync(s => s.Id == id);

            return speaker is null ? Results.NotFound() : Results.Ok(speaker);
        });
    }
}
