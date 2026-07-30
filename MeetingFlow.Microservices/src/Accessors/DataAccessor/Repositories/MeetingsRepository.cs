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
