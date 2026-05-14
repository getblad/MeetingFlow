namespace MeetingFlow.Api.Models;

public class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? InternalContactName { get; set; }
    public string? InternalContactPhone { get; set; }
    public List<Meeting> Meetings { get; set; } = new();
}
