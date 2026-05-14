using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages.Admin;

// Educational baseline:
// This page uses EF Core entities directly, including InternalNotes and AdminOnlyCode.
// In production, prefer an AdminMeetingsViewModel.
public class MeetingsModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public MeetingsModel(MeetingFlowDbContext db) => _db = db;

    public List<Meeting> Meetings { get; set; } = new();

    public async Task OnGetAsync()
    {
        Meetings = await _db.Meetings
            .Include(e => e.Venue)
            .Include(e => e.Registrations)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}
