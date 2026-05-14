using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages.Speakers;

// Educational baseline:
// This page uses the full Speaker entity directly including Email, Phone, InternalNotes.
// In production, prefer a SpeakerProfileViewModel that excludes sensitive/internal fields.
public class DetailsModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public DetailsModel(MeetingFlowDbContext db) => _db = db;

    public Speaker? Speaker { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Speaker = await _db.Speakers
            .Include(s => s.Sessions)
            .FirstOrDefaultAsync(s => s.Id == id);

        return Page();
    }
}
