namespace SchedulingEngine.Models;

// Intentionally redeclared. Manager sends the full Meeting entity to /scheduling/check-capacity
// even though the engine only needs venue capacity and current registration count.
public class Meeting
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? InternalNotes { get; set; }
    public string? AdminOnlyCode { get; set; }
    public Guid VenueId { get; set; }
}
