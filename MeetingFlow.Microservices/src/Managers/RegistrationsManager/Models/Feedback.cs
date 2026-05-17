namespace RegistrationsManager.Models;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid AttendeeId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ModerationNotes { get; set; }
}
