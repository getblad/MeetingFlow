namespace DataAccessor.Models;

public class Attendee
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? InternalNotes { get; set; }
    public List<Registration> Registrations { get; set; } = new();
    public List<Feedback> Feedback { get; set; } = new();
}
