using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages.Meetings;

// Educational baseline:
// This page uses the full Meeting entity with all navigation properties.
// In production, prefer an MeetingDetailsViewModel with only the fields needed for display.
public class DetailsModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public DetailsModel(MeetingFlowDbContext db) => _db = db;

    public Meeting? Meeting { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        // Educational baseline:
        // Eagerly loading everything. The view gets the entire entity graph including internal fields.
        Meeting = await _db.Meetings
            .Include(e => e.Venue)
            .Include(e => e.Sessions).ThenInclude(s => s.Speaker)
            .Include(e => e.Registrations)
            .Include(e => e.Feedback).ThenInclude(f => f.Attendee)
            .FirstOrDefaultAsync(e => e.Id == id);

        return Page();
    }
}
