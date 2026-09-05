using MeetingFlow.Monolith.Models;

namespace MeetingFlow.KosherEvals.Tests.Models;

public class CaseEvaluation
{
    // Report data only. All comparisons are performed in the test.
    public required KosherTestCase Case { get; init; }
    public required DishAssessmentItem Actual { get; init; }
    public required JudgeResult Judgment { get; init; }

    public required bool CodePassed { get; init; }
    public required bool JudgePassed { get; init; }
    public required bool Passed { get; init; }
}
