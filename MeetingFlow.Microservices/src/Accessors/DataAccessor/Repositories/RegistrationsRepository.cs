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

    public Task<int> DeleteRegistrationsByAttendeeAsync(Guid attendeeId) =>
        _db.Registrations
            .Where(item => item.AttendeeId == attendeeId)
            .ExecuteDeleteAsync();

    public async Task<Attendee> CreateAttendeeAsync(Attendee attendee)
    {
        _db.Attendees.Add(attendee);
        await _db.SaveChangesAsync();
        return attendee;
    }

    public async Task<DeleteResult> DeleteAttendeeAsync(Guid id)
    {
        var attendee = await _db.Attendees.FirstOrDefaultAsync(item => item.Id == id);
        if (attendee is null) return DeleteResult.NotFound;

        var hasDependencies = await _db.Registrations.AnyAsync(item => item.AttendeeId == id)
            || await _db.Feedback.AnyAsync(item => item.AttendeeId == id);
        if (hasDependencies) return DeleteResult.HasDependencies;

        _db.Attendees.Remove(attendee);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    public Task<Attendee?> GetAttendeeAsync(Guid id) =>
        _db.Attendees.FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<Attendee>> GetAllAttendeesAsync() =>
        _db.Attendees.ToListAsync();
}
