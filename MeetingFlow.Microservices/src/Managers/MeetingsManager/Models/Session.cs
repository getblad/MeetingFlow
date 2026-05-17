namespace MeetingsManager.Models;

public class Session
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid SpeakerId { get; set; }
    public Speaker? Speaker { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}
