using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace MeetingFlow.Api.Tests.UnitTestingLecture.Exercise.Tests;

// ✏️ EXERCISE: Orchestration tests for RegistrationServiceExercise.

public class RegistrationServiceExerciseTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly SpyRepository _repository = new();
    private readonly SpyNotifications _notifications = new();
    private readonly RegistrationServiceExercise _sut;

    public RegistrationServiceExerciseTests()
    {
        _time.SetUtcNow(new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero));
        _sut = new RegistrationServiceExercise(_repository, _notifications, _time);
    }

    [Fact]
    public async Task Missing_meeting_returns_not_found()
    {
        // Arrange
        _repository.MeetingToReturn = null;

        // Act
        var result = await _sut.RegisterAsync(Guid.NewGuid(), Guid.NewGuid(), "General");

        // Assert
        Assert.IsType<RegistrationResult.NotFound>(result);
    }

    [Fact(Skip = "Remove Skip after replacing DateTimeOffset.UtcNow with _timeProvider.GetUtcNow()")]
    public async Task Valid_registration_uses_deterministic_timestamp()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        _repository.MeetingToReturn = new MeetingInfo(meetingId, "Published", 5, 100);

        // Act
        var result = await _sut.RegisterAsync(meetingId, attendeeId, "General");

        // Assert
        var created = Assert.IsType<RegistrationResult.Created>(result);
        Assert.Equal(_time.GetUtcNow(), created.RegisteredAt);
    }

    // ✏️ EXERCISE (3): Add a test proving that a rejected registration does NOT
    //   send a notification. Hint:
    //   - Configure _repository.MeetingToReturn with a Draft meeting
    //   - Call RegisterAsync
    //   - Assert that _notifications.NotifiedMeetingId is null
    //
    // [Fact]
    // public async Task Rejected_registration_does_not_send_notification()
    // {
    //     ...
    // }

    // ── Hand-written test doubles (provided) ────────────────────────

    private class SpyRepository : IMeetingRepository
    {
        public MeetingInfo? MeetingToReturn { get; set; }
        public Guid? SavedMeetingId { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<MeetingInfo?> GetMeetingAsync(Guid meetingId, CancellationToken ct)
        {
            ReceivedCancellationToken = ct;
            return Task.FromResult(MeetingToReturn);
        }

        public Task SaveRegistrationAsync(
            Guid meetingId, Guid attendeeId, DateTimeOffset registeredAt,
            string ticketType, CancellationToken ct)
        {
            SavedMeetingId = meetingId;
            ReceivedCancellationToken = ct;
            return Task.CompletedTask;
        }
    }

    private class SpyNotifications : INotificationGateway
    {
        public Guid? NotifiedMeetingId { get; private set; }

        public Task SendRegistrationConfirmationAsync(
            Guid meetingId, Guid attendeeId, CancellationToken ct)
        {
            NotifiedMeetingId = meetingId;
            return Task.CompletedTask;
        }
    }
}
