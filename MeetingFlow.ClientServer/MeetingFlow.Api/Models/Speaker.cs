namespace MeetingFlow.Api.Models;

public class Speaker
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? InternalNotes { get; set; }
    public List<Session> Sessions { get; set; } = new();
}
