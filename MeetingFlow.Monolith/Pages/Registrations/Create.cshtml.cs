using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages.Registrations;

// Educational baseline:
// This page binds directly to the Registration entity.
// In production, prefer a dedicated input model (e.g. CreateRegistrationInput)
// to prevent over-posting and control which fields the user can set.
public class CreateModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public CreateModel(MeetingFlowDbContext db) => _db = db;

    [BindProperty]
    public Registration Registration { get; set; } = new();

    public List<Meeting> AvailableMeetings { get; set; } = new();
    public List<Attendee> AvailableAttendees { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadFormData();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Educational baseline:
        // Binding directly to the entity means the user could potentially
        // set InternalPaymentReference or any other field via form manipulation.
        Registration.Id = Guid.NewGuid();
        Registration.RegisteredAt = DateTimeOffset.UtcNow;
        Registration.PaymentStatus = "Pending";

        _db.Registrations.Add(Registration);
        await _db.SaveChangesAsync();

        SuccessMessage = "Registration created successfully!";
        await LoadFormData();
        return Page();
    }

    private async Task LoadFormData()
    {
        AvailableMeetings = await _db.Meetings
            .Where(e => e.Status == "Published")
            .OrderBy(e => e.Title)
            .ToListAsync();
        AvailableAttendees = await _db.Attendees
            .OrderBy(a => a.FullName)
            .ToListAsync();
    }
}
