using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages;

// Educational baseline:
// This page passes EF Core entities directly to the view and uses inline aggregation.
// In production, prefer a DashboardViewModel with pre-computed properties.
public class DashboardModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public DashboardModel(MeetingFlowDbContext db) => _db = db;

    public int TotalMeetings { get; set; }
    public int TotalRegistrations { get; set; }
    public double AverageFeedbackRating { get; set; }
    public int TotalSpeakers { get; set; }
    public List<Meeting> UpcomingMeetings { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalMeetings = await _db.Meetings.CountAsync();
        TotalRegistrations = await _db.Registrations.CountAsync();
        AverageFeedbackRating = await _db.Feedback.AnyAsync()
            ? await _db.Feedback.AverageAsync(f => f.Rating)
            : 0;
        TotalSpeakers = await _db.Speakers.CountAsync();

        // Educational baseline:
        // Loading full Meeting entities with navigation properties just for the dashboard.
        // A DashboardViewModel would only include the fields displayed here.
        UpcomingMeetings = await _db.Meetings
            .Include(e => e.Venue)
            .Include(e => e.Registrations)
            .Where(e => e.StartsAt > DateTimeOffset.UtcNow && e.Status == "Published")
            .OrderBy(e => e.StartsAt)
            .Take(5)
            .ToListAsync();
    }
}
