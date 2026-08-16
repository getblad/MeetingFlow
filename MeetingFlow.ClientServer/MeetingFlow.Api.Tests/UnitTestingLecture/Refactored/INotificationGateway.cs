namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

public interface INotificationGateway
{
    Task SendRegistrationConfirmationAsync(
        Guid meetingId, Guid attendeeId, CancellationToken ct = default);
}
