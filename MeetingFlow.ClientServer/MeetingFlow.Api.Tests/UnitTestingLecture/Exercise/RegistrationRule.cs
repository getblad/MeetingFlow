namespace MeetingFlow.Api.Tests.UnitTestingLecture.Exercise;

// ✏️ EXERCISE (1): Implement this pure validation method.
// Extract the registration logic from RegistrationServiceExercise.RegisterAsync
// into this static method.
//
// Rules (these are the rules the API *should* enforce — RegistrationsEndpoints.cs
// does not check any of them yet):
//   - If meeting.Status is NOT "Published" → Rejected("Meeting is not open for registration")
//   - If meeting.RegistrationCount >= meeting.VenueCapacity → Rejected("Meeting is full")
//   - Otherwise → Allowed
public static class RegistrationRule
{
    public static RegistrationDecision Validate(MeetingInfo meeting)
    {
        // TODO: implement
        throw new NotImplementedException("Complete this exercise");
    }
}
