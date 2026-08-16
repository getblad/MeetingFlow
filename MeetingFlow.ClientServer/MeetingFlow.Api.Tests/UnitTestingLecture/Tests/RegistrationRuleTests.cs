using MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;
using Xunit;

namespace MeetingFlow.Api.Tests.UnitTestingLecture.Tests;

// ✅ Parameterized unit tests for the pure registration rule.
// No repository, no HTTP client, no mocks — just input → output.

public class RegistrationRuleTests
{
    // 'scenario' is deliberately unused in the body: xUnit builds each case's
    // display name from its MemberData arguments, so this string is what labels
    // the case in the test runner. xUnit1026 cannot see that, hence the suppression.
#pragma warning disable xUnit1026
    [Theory]
    [MemberData(nameof(ValidationCases))]
    public void Validate_returns_correct_decision(
        string scenario,
        MeetingInfo meeting,
        RegistrationDecision expected)
#pragma warning restore xUnit1026
    {
        // Act
        var decision = RegistrationRule.Validate(meeting);

        // Assert
        Assert.Equal(expected, decision);
    }

    public static IEnumerable<object[]> ValidationCases()
    {
        yield return
        [
            "Published meeting with capacity → allowed",
            new MeetingInfo(Guid.NewGuid(), "Published", RegistrationCount: 5, VenueCapacity: 100),
            new RegistrationDecision.Allowed()
        ];

        yield return
        [
            "Draft meeting → rejected",
            new MeetingInfo(Guid.NewGuid(), "Draft", RegistrationCount: 0, VenueCapacity: 100),
            new RegistrationDecision.Rejected("Meeting is not open for registration")
        ];

        yield return
        [
            "Cancelled meeting → rejected",
            new MeetingInfo(Guid.NewGuid(), "Cancelled", RegistrationCount: 0, VenueCapacity: 100),
            new RegistrationDecision.Rejected("Meeting is not open for registration")
        ];

        yield return
        [
            "Full meeting → rejected",
            new MeetingInfo(Guid.NewGuid(), "Published", RegistrationCount: 100, VenueCapacity: 100),
            new RegistrationDecision.Rejected("Meeting is full")
        ];
    }
}
