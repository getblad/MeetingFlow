using DataAccessor.Data;
using DataAccessor.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessor.Repositories;

public class MeetingsRepository
{
    readonly MeetingFlowDbContext _db;
    public MeetingsRepository(MeetingFlowDbContext db) => _db = db;

    public Task<List<Meeting>> GetAllAsync() =>
        _db.Meetings
            .Include(m => m.Venue)
            .ToListAsync();

    public Task<Meeting?> GetByIdAsync(Guid id) =>
        _db.Meetings
            .Include(m => m.Venue)
            .Include(m => m.Sessions).ThenInclude(s => s.Speaker)
            .Include(m => m.Registrations).ThenInclude(r => r.Attendee)
            .Include(m => m.Feedback).ThenInclude(f => f.Attendee)
            .FirstOrDefaultAsync(m => m.Id == id);

    public Task<Meeting?> GetRegistrationContextAsync(Guid id) =>
        _db.Meetings
            .Include(meeting => meeting.Venue)
            .FirstOrDefaultAsync(meeting => meeting.Id == id);

    public async Task<Venue> CreateVenueAsync(Venue venue)
    {
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();
        return venue;
    }

    public async Task<Meeting?> CreateMeetingAsync(Meeting meeting)
    {
        if (!await _db.Venues.AnyAsync(venue => venue.Id == meeting.VenueId))
        {
            return null;
        }

        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(meeting.Id);
    }

    public async Task<DeleteResult> DeleteVenueAsync(Guid id)
    {
        var venue = await _db.Venues.FirstOrDefaultAsync(item => item.Id == id);
        if (venue is null) return DeleteResult.NotFound;
        if (await _db.Meetings.AnyAsync(meeting => meeting.VenueId == id))
            return DeleteResult.HasDependencies;

        _db.Venues.Remove(venue);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    public async Task<DeleteResult> DeleteMeetingAsync(Guid id)
    {
        var meeting = await _db.Meetings.FirstOrDefaultAsync(item => item.Id == id);
        if (meeting is null) return DeleteResult.NotFound;

        var hasDependencies = await _db.Sessions.AnyAsync(item => item.MeetingId == id)
            || await _db.Registrations.AnyAsync(item => item.MeetingId == id)
            || await _db.Feedback.AnyAsync(item => item.MeetingId == id)
            || await _db.MeetingTasks.AnyAsync(item => item.MeetingId == id);
        if (hasDependencies) return DeleteResult.HasDependencies;

        _db.Meetings.Remove(meeting);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    public async Task<Meeting?> UpdateAsync(
        Guid id,
        string title,
        string description,
        string status,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid venueId)
    {
        var existing = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == id);
        if (existing is null) return null;

        existing.Title = title;
        existing.Description = description;
        existing.Status = status;
        existing.StartsAt = startsAt;
        existing.EndsAt = endsAt;
        existing.VenueId = venueId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public Task<List<Session>> GetSessionsByMeetingAsync(Guid meetingId) =>
        _db.Sessions
            .Include(s => s.Speaker)
            .Where(s => s.MeetingId == meetingId)
            .ToListAsync();

    public Task<List<Speaker>> GetAllSpeakersAsync() =>
        _db.Speakers.Include(s => s.Sessions).ToListAsync();

    public Task<Speaker?> GetSpeakerByIdAsync(Guid id) =>
        _db.Speakers.Include(s => s.Sessions).FirstOrDefaultAsync(s => s.Id == id);
}
