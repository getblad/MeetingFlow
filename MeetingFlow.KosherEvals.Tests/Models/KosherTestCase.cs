using MeetingFlow.Monolith.Models;

namespace MeetingFlow.KosherEvals.Tests.Models;

public sealed record KosherTestCase(
    string Id,
    string Dish,
    DishAssessmentStatus ExpectedStatus,
    // null means no additional clarification is needed.
    string? ExpectedClarification,
    string ExpectedReasoning);
