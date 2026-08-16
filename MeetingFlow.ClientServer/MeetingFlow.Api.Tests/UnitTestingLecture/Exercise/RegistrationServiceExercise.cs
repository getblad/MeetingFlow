namespace MeetingFlow.Api.Tests.UnitTestingLecture.Exercise;

// ✏️ This service has two problems to fix:
//   (1) The validation logic is inline — extract it into RegistrationRule.Validate
//   (2) It uses DateTimeOffset.UtcNow — replace with _timeProvider.GetUtcNow()

public class RegistrationServiceExercise
{
    private readonly IMeetingRepository _repository;
    private readonly INotificationGateway _notifications;
    private readonly TimeProvider _timeProvider; // ← TODO (2): use this instead of DateTimeOffset.UtcNow

    public RegistrationServiceExercise(
        IMeetingRepository repository,
        INotificationGateway notifications,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    public async Task<RegistrationResult> RegisterAsync(
        Guid meetingId, Guid attendeeId, string ticketType,
        CancellationToken ct = default)
    {
        var meeting = await _repository.GetMeetingAsync(meetingId, ct);
        if (meeting is null)
            return new RegistrationResult.NotFound();

        // TODO (1): Replace this block with:
        //   var decision = RegistrationRule.Validate(meeting);
        //   if (decision is RegistrationDecision.Rejected rejected)
        //       return new RegistrationResult.Rejected(rejected.Reason);
        if (meeting.Status != "Published")
            return new RegistrationResult.Rejected("Meeting is not open for registration");
        if (meeting.RegistrationCount >= meeting.VenueCapacity)
            return new RegistrationResult.Rejected("Meeting is full");

        // TODO (2): Replace DateTimeOffset.UtcNow with _timeProvider.GetUtcNow()
        var registeredAt = DateTimeOffset.UtcNow;

        await _repository.SaveRegistrationAsync(meetingId, attendeeId, registeredAt, ticketType, ct);
        await _notifications.SendRegistrationConfirmationAsync(meetingId, attendeeId, ct);

        return new RegistrationResult.Created(meetingId, attendeeId, registeredAt);
    }
}
