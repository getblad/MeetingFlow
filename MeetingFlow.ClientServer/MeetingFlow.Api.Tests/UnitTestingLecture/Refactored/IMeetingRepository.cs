namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

public interface IMeetingRepository
{
    Task<MeetingInfo?> GetMeetingAsync(Guid meetingId, CancellationToken ct = default);

    Task SaveRegistrationAsync(
        Guid meetingId, Guid attendeeId, DateTimeOffset registeredAt,
        string ticketType, CancellationToken ct = default);
}
