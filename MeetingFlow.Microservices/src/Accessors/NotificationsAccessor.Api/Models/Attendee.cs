namespace NotificationsAccessor.Models;

// Intentionally redeclared. Callers send the full Attendee entity to /notifications/send
// even though we only need the email address. Lives here as its own near-duplicate of
// the Attendee class declared inside DataAccessor.Api. They will drift over time.
public class Attendee
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? InternalNotes { get; set; }
}
