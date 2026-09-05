namespace MeetingFlow.KosherEvals.Tests.Models;

public sealed record EvalReport(
    DateTimeOffset StartedAtUtc,
    string Model,
    string JudgeModel,
    int TotalCases,
    List<CaseEvaluation> Cases,
    string? Error = null)
{
    public int CompletedCases => Cases.Count;
    public int PassedCases => Cases.Count(item => item.Passed);

    // Average only received scores. No scores means no average.
    public double? AverageScore => Cases.Count == 0 ? null : Cases.Average(item => item.Judgment.Score);

    public string Conclusion => Error is not null || CompletedCases != TotalCases
        ? "Incomplete"
        : PassedCases == TotalCases ? "Passed" : "Failed";
}
