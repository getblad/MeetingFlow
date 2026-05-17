namespace SchedulingEngine.Models;

// Intentionally redeclared. Manager sends the full Session entity to /scheduling/check-conflict
// even though the engine only needs StartsAt, EndsAt, RoomName, and MeetingId. Carrying
// InternalNotes, SpeakerId, etc. across the wire is the smell to refactor.
public class Session
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid SpeakerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}
