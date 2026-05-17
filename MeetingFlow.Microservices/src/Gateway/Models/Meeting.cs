namespace Gateway.Models;

// Yet another redeclaration. Gateway is supposed to be the public edge, but it
// passes through whatever the manager returns and has its own copy of the entity
// shape. So internal fields like InternalNotes/AdminOnlyCode leak straight to
// public HTTP clients on port 8080.
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
