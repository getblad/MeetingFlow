namespace MeetingFlow.Api.Tests.UnitTestingLecture.Refactored;

// Lightweight input for the pure validation rule.
// Carries only the meeting facts needed to decide whether registration is allowed.
public record MeetingInfo(
    Guid Id,
    string Status,
    int RegistrationCount,
    int VenueCapacity);
