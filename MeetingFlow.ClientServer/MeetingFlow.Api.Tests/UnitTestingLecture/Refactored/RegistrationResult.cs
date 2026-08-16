namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

// Explicit result type for the orchestration service.
public abstract record RegistrationResult
{
    private RegistrationResult() { }

    public sealed record Created(Guid MeetingId, Guid AttendeeId, DateTimeOffset RegisteredAt) : RegistrationResult;
    public sealed record Rejected(string Reason) : RegistrationResult;
    public sealed record NotFound : RegistrationResult;
}
