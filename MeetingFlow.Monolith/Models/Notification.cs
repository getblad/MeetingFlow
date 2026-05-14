namespace MeetingFlow.Monolith.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid AttendeeId { get; set; }
    public Attendee Attendee { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RawPayloadJson { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
