using DataAccessor.Data;
using DataAccessor.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessor.Repositories;

public class FeedbackRepository
{
    readonly MeetingFlowDbContext _db;
    public FeedbackRepository(MeetingFlowDbContext db) => _db = db;

    public Task<List<Feedback>> GetByMeetingAsync(Guid meetingId) =>
        _db.Feedback
            .Include(f => f.Attendee)
            .Where(f => f.MeetingId == meetingId)
            .ToListAsync();

    public async Task<Feedback> CreateAsync(Feedback feedback)
    {
        _db.Feedback.Add(feedback);
        await _db.SaveChangesAsync();
        return feedback;
    }
}
