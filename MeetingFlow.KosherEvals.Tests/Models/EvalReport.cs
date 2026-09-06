namespace MeetingFlow.KosherEvals.Tests.Models;

public sealed record EvalReport(
    DateTimeOffset StartedAtUtc,
    string Model,
    string JudgeModel,
    int TotalCases,
    int MaximumExplanationLength,
    List<CaseEvaluation> Cases,
    string? Error = null)
{
    public int CompletedCases => Cases.Count;
    public int PassedCases => Cases.Count(item => item.Passed);

    // Average only received scores. No scores means no average.
    public double? AverageScore
    {
        get
        {
            var scores = Cases
                .Where(item => item.Judgment is not null)
                .Select(item => item.Judgment!.Score)
                .ToList();

            return scores.Count == 0 ? null : scores.Average();
        }
    }

    public string Conclusion => Error is not null || CompletedCases != TotalCases
        ? "Incomplete"
        : PassedCases == TotalCases ? "Passed" : "Failed";
}
