namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

// Result type for the pure registration rule.
// Uses a discriminated-union pattern instead of exceptions.
public abstract record RegistrationDecision
{
    private RegistrationDecision() { }

    public sealed record Allowed : RegistrationDecision;
    public sealed record Rejected(string Reason) : RegistrationDecision;
}
