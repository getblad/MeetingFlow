using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Pages.Admin;

// Educational baseline:
// This page uses AuditLogEntry entities directly.
// TechnicalDetails can contain SQL, IPs, and sensitive system info.
// In production, prefer an AuditLogViewModel or filter sensitive details.
public class AuditLogModel : PageModel
{
    private readonly MeetingFlowDbContext _db;
    public AuditLogModel(MeetingFlowDbContext db) => _db = db;

    public List<AuditLogEntry> AuditLogEntries { get; set; } = new();

    public async Task OnGetAsync()
    {
        var entries = await _db.AuditLogEntries
            .ToListAsync();
        AuditLogEntries = entries.OrderByDescending(a => a.CreatedAt).ToList();
    }
}
