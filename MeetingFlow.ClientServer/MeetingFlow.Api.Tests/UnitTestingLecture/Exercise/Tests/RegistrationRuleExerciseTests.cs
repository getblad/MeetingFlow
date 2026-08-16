using Xunit;

namespace MeetingFlow.Api.Tests.UnitTestingLecture.Exercise.Tests;

// ✏️ EXERCISE: These tests verify the pure RegistrationRule.
// They will fail until you implement RegistrationRule.Validate in RegistrationRule.cs.

public class RegistrationRuleExerciseTests
{
    [Fact(Skip = "Remove Skip after implementing RegistrationRule.Validate")]
    public void Published_meeting_with_capacity_is_allowed()
    {
        var meeting = new MeetingInfo(Guid.NewGuid(), "Published", RegistrationCount: 5, VenueCapacity: 100);

        var decision = RegistrationRule.Validate(meeting);

        Assert.IsType<RegistrationDecision.Allowed>(decision);
    }

    [Fact(Skip = "Remove Skip after implementing RegistrationRule.Validate")]
    public void Draft_meeting_is_rejected()
    {
        var meeting = new MeetingInfo(Guid.NewGuid(), "Draft", RegistrationCount: 0, VenueCapacity: 100);

        var decision = RegistrationRule.Validate(meeting);

        var rejected = Assert.IsType<RegistrationDecision.Rejected>(decision);
        Assert.Equal("Meeting is not open for registration", rejected.Reason);
    }

    [Fact(Skip = "Remove Skip after implementing RegistrationRule.Validate")]
    public void Full_meeting_is_rejected()
    {
        var meeting = new MeetingInfo(Guid.NewGuid(), "Published", RegistrationCount: 100, VenueCapacity: 100);

        var decision = RegistrationRule.Validate(meeting);

        var rejected = Assert.IsType<RegistrationDecision.Rejected>(decision);
        Assert.Equal("Meeting is full", rejected.Reason);
    }
}
