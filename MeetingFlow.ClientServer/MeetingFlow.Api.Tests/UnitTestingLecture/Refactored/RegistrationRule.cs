namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

// ✅ Pure function: no I/O, no side effects, no dependencies.
// Extracts the validation logic that lives inline in RegistrationsEndpoints.cs today.
// Testable with plain unit tests — no mocks needed.
public static class RegistrationRule
{
    public static RegistrationDecision Validate(MeetingInfo meeting)
    {
        if (meeting.Status != "Published")
            return new RegistrationDecision.Rejected("Meeting is not open for registration");

        if (meeting.RegistrationCount >= meeting.VenueCapacity)
            return new RegistrationDecision.Rejected("Meeting is full");

        return new RegistrationDecision.Allowed();
    }
}
