namespace MeetingFlow.Api.Tests.UnitTestingLecture.Exercise;

// Types shared by the exercise files — already provided, no changes needed.

public record MeetingInfo(
    Guid Id,
    string Status,
    int RegistrationCount,
    int VenueCapacity);

public abstract record RegistrationDecision
{
    private RegistrationDecision() { }
    public sealed record Allowed : RegistrationDecision;
    public sealed record Rejected(string Reason) : RegistrationDecision;
}

public abstract record RegistrationResult
{
    private RegistrationResult() { }
    public sealed record Created(Guid MeetingId, Guid AttendeeId, DateTimeOffset RegisteredAt) : RegistrationResult;
    public sealed record Rejected(string Reason) : RegistrationResult;
    public sealed record NotFound : RegistrationResult;
}

public interface IMeetingRepository
{
    Task<MeetingInfo?> GetMeetingAsync(Guid meetingId, CancellationToken ct = default);
    Task SaveRegistrationAsync(
        Guid meetingId, Guid attendeeId, DateTimeOffset registeredAt,
        string ticketType, CancellationToken ct = default);
}

public interface INotificationGateway
{
    Task SendRegistrationConfirmationAsync(
        Guid meetingId, Guid attendeeId, CancellationToken ct = default);
}
