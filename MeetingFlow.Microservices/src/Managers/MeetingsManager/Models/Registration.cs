namespace MeetingsManager.Models;

// Yet another Registration redeclaration — different from the one in RegistrationsManager.
// Drift between these two manager-local copies is exactly the cross-service smell.
public class Registration
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid AttendeeId { get; set; }
    public Attendee? Attendee { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public string TicketType { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? InternalPaymentReference { get; set; }
}

public class Attendee
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? InternalNotes { get; set; }
}

public class Feedback
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid AttendeeId { get; set; }
    public Attendee? Attendee { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ModerationNotes { get; set; }
}
