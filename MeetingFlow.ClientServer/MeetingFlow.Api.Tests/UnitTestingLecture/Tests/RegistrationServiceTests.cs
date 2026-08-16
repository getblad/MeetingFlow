using Microsoft.Extensions.Time.Testing;
using MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;
using Xunit;

namespace MeetingFlow.Api.Tests.UnitTestingLecture.Tests;

// ✅ Orchestration tests with hand-written fakes/spies.
// Verify that the service coordinates the right calls in the right order.

public class RegistrationServiceTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly SpyRepository _repository = new();
    private readonly SpyNotifications _notifications = new();
    private readonly RegistrationService _sut;

    public RegistrationServiceTests()
    {
        _time.SetUtcNow(new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero));
        _sut = new RegistrationService(_repository, _notifications, _time);
    }

    [Fact]
    public async Task Valid_registration_is_timestamped_saved_and_notified()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        _repository.MeetingToReturn = new MeetingInfo(meetingId, "Published", 5, 100);

        // Act
        var result = await _sut.RegisterAsync(meetingId, attendeeId, "General");

        // Assert
        var created = Assert.IsType<RegistrationResult.Created>(result);
        Assert.Equal(meetingId, created.MeetingId);
        Assert.Equal(attendeeId, created.AttendeeId);
        Assert.Equal(_time.GetUtcNow(), created.RegisteredAt);
        Assert.Equal(meetingId, _repository.SavedMeetingId);
        Assert.Equal(meetingId, _notifications.NotifiedMeetingId);
    }

    [Fact]
    public async Task Rejected_registration_is_not_saved()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        _repository.MeetingToReturn = new MeetingInfo(meetingId, "Draft", 0, 100);

        // Act
        var result = await _sut.RegisterAsync(meetingId, Guid.NewGuid(), "General");

        // Assert
        var rejected = Assert.IsType<RegistrationResult.Rejected>(result);
        Assert.Equal("Meeting is not open for registration", rejected.Reason);
        Assert.Null(_repository.SavedMeetingId); // never saved
    }

    [Fact]
    public async Task Rejected_registration_does_not_send_notification()
    {
        // Arrange — meeting at full capacity
        var meetingId = Guid.NewGuid();
        _repository.MeetingToReturn = new MeetingInfo(meetingId, "Published", 100, 100);

        // Act
        await _sut.RegisterAsync(meetingId, Guid.NewGuid(), "General");

        // Assert — notification was never sent
        Assert.Null(_notifications.NotifiedMeetingId);
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

    [Fact]
    public async Task CancellationToken_is_forwarded_to_repository()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        _repository.MeetingToReturn = new MeetingInfo(meetingId, "Published", 5, 100);
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.RegisterAsync(meetingId, Guid.NewGuid(), "General", cts.Token);

        // Assert
        Assert.Equal(cts.Token, _repository.ReceivedCancellationToken);
    }

    // ── Hand-written test doubles ──────────────────────────────────

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
