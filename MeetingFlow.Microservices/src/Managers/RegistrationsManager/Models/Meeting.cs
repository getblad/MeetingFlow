namespace RegistrationsManager.Models;

// Redeclared AGAIN, separately from MeetingsManager's Meeting. RegistrationsManager
// needs Capacity for the capacity check and the inline pricing logic — and fetches
// the entire Meeting graph just for that.
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
    public Venue? Venue { get; set; }
}

public class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
