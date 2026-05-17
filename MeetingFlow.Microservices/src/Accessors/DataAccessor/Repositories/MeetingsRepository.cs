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
            .Include(m => m.Sessions).ThenInclude(s => s.Speaker)
            .ToListAsync();

    public Task<Meeting?> GetByIdAsync(Guid id) =>
        _db.Meetings
            .Include(m => m.Venue)
            .Include(m => m.Sessions).ThenInclude(s => s.Speaker)
            .Include(m => m.Registrations).ThenInclude(r => r.Attendee)
            .Include(m => m.Feedback).ThenInclude(f => f.Attendee)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Meeting> UpsertAsync(Meeting meeting)
    {
        var existing = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == meeting.Id);
        if (existing is null)
        {
            _db.Meetings.Add(meeting);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(meeting);
        }
        await _db.SaveChangesAsync();
        return meeting;
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
