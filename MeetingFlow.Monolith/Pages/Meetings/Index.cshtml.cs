using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages.Meetings;

// Educational baseline:
// This page uses EF Core entities directly.
// In production, prefer a page-specific view model or DTO.
public class IndexModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public IndexModel(MeetingFlowDbContext db) => _db = db;

    public List<Meeting> Meetings { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Educational baseline:
        // Full Meeting entities are loaded including internal fields (InternalNotes, AdminOnlyCode).
        // The view only displays a few fields, but the entire entity is available in memory.
        var meetings = await _db.Meetings
            .Include(e => e.Venue)
            .ToListAsync();
        Meetings = meetings.OrderBy(e => e.StartsAt).ToList();
    }
}
