namespace MeetingFlow.Monolith.Evals;

public sealed record CaseResult(
    EvalCase Case,
    DeterministicCheckResult DeterministicResult,
    JudgeVerdict? Judge,
    string? RunError);

public static class ReportWriter
{
    public static void Write(
        string path,
        string evaluatedModel,
        string judgeModel,
        IReadOnlyList<CaseResult> results)
    {
        var passing = results.Count(IsFullyPassing);
        var judged = results.Where(r => r.Judge is not null).ToList();
        var averageScore = judged.Count == 0 ? 0 : judged.Average(r => r.Judge!.Score);

        var lines = new List<string>
        {
            "# Kosher Check Eval Report",
            "",
            $"- Run date: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}",
            $"- Evaluated model: `{evaluatedModel}`",
            $"- Judge model: `{judgeModel}`",
            $"- Cases run: {results.Count}",
            $"- Passing cases: {passing} / {results.Count}",
            $"- Average judge score: {averageScore:F2} / 5",
            "",
            "## Results",
            "",
            "| Case | Dishes | Deterministic | Judge score | Judge passed | Notes |",
            "|---|---|---|---|---|---|"
        };

        foreach (var result in results)
        {
            var dishText = Escape(string.Join("; ", result.Case.Dishes));
            var deterministic = result.DeterministicResult.Passed
                ? "PASS"
                : Escape($"FAIL: {string.Join("; ", result.DeterministicResult.FailureReasons)}");
            var judgeScore = result.Judge is null ? "-" : $"{result.Judge.Score}/{result.Judge.MaxScore}";
            var judgePassed = result.Judge is null ? "-" : (result.Judge.Passed ? "PASS" : "FAIL");
            var notes = Escape(result.Judge is null
                ? result.RunError ?? "not judged"
                : string.Join("; ", result.Judge.Reasons));

            lines.Add($"| {result.Case.CaseId} | {dishText} | {deterministic} | {judgeScore} | {judgePassed} | {notes} |");
        }

        lines.Add("");
        lines.Add("## Conclusion");
        lines.Add("");

        var failedCases = results.Where(r => !IsFullyPassing(r)).ToList();
        if (failedCases.Count == 0)
        {
            lines.Add("All cases passed both the deterministic checks and the judge's rubric in this run.");
        }
        else
        {
            lines.Add($"{failedCases.Count} of {results.Count} case(s) did not fully pass:");
            foreach (var failed in failedCases)
            {
                var reason = failed.RunError
                    ?? (!failed.DeterministicResult.Passed
                        ? string.Join("; ", failed.DeterministicResult.FailureReasons)
                        : string.Join("; ", failed.Judge?.Reasons ?? []));
                lines.Add($"- `{failed.Case.CaseId}`: {reason}");
            }
        }

        File.WriteAllLines(path, lines);
    }

    private static bool IsFullyPassing(CaseResult result) =>
        result.DeterministicResult.Passed && (result.Judge?.Passed ?? false);

    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\n", " ");
}
