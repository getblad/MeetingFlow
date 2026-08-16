namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

// ✅ AFTER: Testable orchestration service.
// All dependencies are explicit via constructor injection.
// Business decisions are delegated to the pure RegistrationRule.
// Time is controlled via TimeProvider.
//
// Compare with the existing RegistrationsEndpoints.cs which does all of this
// inline in the endpoint handler with hidden dependencies.
public class RegistrationService(
    IMeetingRepository repository,
    INotificationGateway notifications,
    TimeProvider timeProvider)
{
    public async Task<RegistrationResult> RegisterAsync(
        Guid meetingId, Guid attendeeId, string ticketType,
        CancellationToken ct = default)
    {
        var meeting = await repository.GetMeetingAsync(meetingId, ct);
        if (meeting is null)
            return new RegistrationResult.NotFound();

        var decision = RegistrationRule.Validate(meeting);
        if (decision is RegistrationDecision.Rejected rejected)
            return new RegistrationResult.Rejected(rejected.Reason);

        var registeredAt = timeProvider.GetUtcNow();
        await repository.SaveRegistrationAsync(meetingId, attendeeId, registeredAt, ticketType, ct);
        await notifications.SendRegistrationConfirmationAsync(meetingId, attendeeId, ct);

        return new RegistrationResult.Created(meetingId, attendeeId, registeredAt);
    }
}
