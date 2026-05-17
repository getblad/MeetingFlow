namespace MeetingsManager.Models;

// Local redeclaration. The DataAccessor returns its own near-identical Meeting type;
// we deserialize it into THIS class. If either side drifts (e.g. Title -> Name), the
// other silently nulls out the field. That silent drift is the teaching smell.
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
    public List<Session> Sessions { get; set; } = new();
    public List<Registration> Registrations { get; set; } = new();
    public List<Feedback> Feedback { get; set; } = new();
}
