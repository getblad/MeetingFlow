using DataAccessor.Data;
using DataAccessor.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessor.Repositories;

public class RegistrationsRepository
{
    readonly MeetingFlowDbContext _db;
    public RegistrationsRepository(MeetingFlowDbContext db) => _db = db;

    public Task<List<Registration>> GetAllAsync() =>
        _db.Registrations
            .Include(r => r.Attendee)
            .Include(r => r.Meeting).ThenInclude(m => m.Venue)
            .ToListAsync();

    public Task<List<Registration>> GetByMeetingAsync(Guid meetingId) =>
        _db.Registrations
            .Include(r => r.Attendee)
            .Where(r => r.MeetingId == meetingId)
            .ToListAsync();

    public async Task<Registration> CreateAsync(Registration registration)
    {
        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();
        return registration;
    }

    public Task<Attendee?> GetAttendeeAsync(Guid id) =>
        _db.Attendees.FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<Attendee>> GetAllAttendeesAsync() =>
        _db.Attendees.ToListAsync();
}
