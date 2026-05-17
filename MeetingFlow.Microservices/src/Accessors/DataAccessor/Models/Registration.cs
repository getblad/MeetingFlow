namespace DataAccessor.Models;

public class Registration
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public Guid AttendeeId { get; set; }
    public Attendee Attendee { get; set; } = null!;
    public DateTimeOffset RegisteredAt { get; set; }
    public string TicketType { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? InternalPaymentReference { get; set; }
}
