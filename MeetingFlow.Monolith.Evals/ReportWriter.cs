using System.Text;

namespace MeetingFlow.Monolith.Evals;

/// <summary>
/// Writes the run report. One file, eval-report.md, holding every item the assignment requires:
/// the run date, the evaluated model, the number of passing cases, the average score, a row for
/// every case, and a conclusion. Detail is written only for the cases that failed, because a
/// full write-up of every passing case buries the result it is supposed to communicate.
/// </summary>
public static class ReportWriter
{
    public static string Write(EvalRunSummary summary, string reportDirectory)
    {
        Directory.CreateDirectory(reportDirectory);

        var path = Path.Combine(reportDirectory, "eval-report.md");
        File.WriteAllText(
            path,
            BuildMarkdown(summary),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return path;
    }

    private static string BuildMarkdown(EvalRunSummary summary)
    {
        var report = new StringBuilder();

        report.AppendLine("# Kosher check eval report");
        report.AppendLine();
        report.AppendLine($"- **Run date:** {summary.RunDate:yyyy-MM-dd HH:mm:ss zzz}");
        report.AppendLine($"- **Evaluated model:** `{summary.EvaluatedModel}` at `{summary.EvaluatedEndpoint}`");
        report.AppendLine($"- **Judge model:** `{summary.JudgeModel}` at `{summary.JudgeEndpoint}`");

        if (summary.JudgeIsSameModel)
        {
            report.AppendLine("  - Note: the judge is the same model as the system under test, so scores may be");
            report.AppendLine("    generous towards its own answers. Set `Eval:JudgeModel` to a different model to avoid this.");
        }

        report.AppendLine($"- **Case file:** `{summary.CasesFile}`");
        report.AppendLine($"- **Cases:** {summary.CaseCount}, at {summary.Repeat} attempt(s) each");
        report.AppendLine($"- **Run duration:** {summary.DurationMs / 1000.0:0.0}s");
        report.AppendLine();

        report.AppendLine("## Summary");
        report.AppendLine();
        report.AppendLine($"- **Passing cases:** {summary.PassedCases} of {summary.CaseCount}");
        report.AppendLine($"- **Average score:** {summary.AverageScore:0.00} of {summary.MaxScore}");
        report.AppendLine($"- **Attempts that failed a code check:** {summary.DeterministicFailures} of {summary.AttemptCount}");
        report.AppendLine($"- **Attempts that passed the code checks but scored below the bar:** {summary.JudgeFailures}");
        report.AppendLine();
        report.AppendLine("Every case is graded twice: by code-only checks on the response contract and the per-dish");
        report.AppendLine("status expectations, and by a language-model judge scoring five criteria against the case");
        report.AppendLine("rubric. A case passes only when every attempt clears both. With more than one attempt per");
        report.AppendLine("case, an inconsistent case counts as failing.");
        report.AppendLine();

        report.AppendLine("## Results by case");
        report.AppendLine();
        report.AppendLine("| Case | Title | Tags | Result | Score | Failed code checks |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var outcome in summary.Cases)
        {
            var failed = outcome.Attempts
                .SelectMany(attempt => attempt.FailedCheckNames)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            report.AppendLine(
                $"| `{outcome.Case.Id}` " +
                $"| {Escape(outcome.Case.Title)} " +
                $"| {string.Join(", ", outcome.Case.Tags)} " +
                $"| {(outcome.Passed ? "pass" : "**FAIL**")} " +
                $"| {outcome.AverageScore:0.0}/{summary.MaxScore} " +
                $"| {(failed.Count == 0 ? "none" : string.Join(", ", failed))} |");
        }

        report.AppendLine();

        report.AppendLine("## Results by tag");
        report.AppendLine();
        report.AppendLine("| Tag | Cases | Passing | Pass rate | Average score |");
        report.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var tag in summary.TagBreakdown)
        {
            report.AppendLine(
                $"| {tag.Tag} | {tag.Cases} | {tag.Passed} | {tag.PassRate:P0} | {tag.AverageScore:0.0}/{summary.MaxScore} |");
        }

        report.AppendLine();

        report.AppendLine("## Conclusion");
        report.AppendLine();
        report.AppendLine(BuildConclusion(summary));
        report.AppendLine();

        AppendFailureDetail(report, summary);

        return report.ToString();
    }

    /// <summary>
    /// Writes the full evidence for failing cases only: what the model answered, which code checks
    /// rejected it, and how the judge scored each criterion.
    /// </summary>
    private static void AppendFailureDetail(StringBuilder report, EvalRunSummary summary)
    {
        var failures = summary.Cases.Where(outcome => !outcome.Passed).ToList();

        report.AppendLine("## Failure detail");
        report.AppendLine();

        if (failures.Count == 0)
        {
            report.AppendLine("Every case passed in this run, so there is nothing to detail.");
            return;
        }

        report.AppendLine($"The {failures.Count} failing case(s) in full. Passing cases are summarised in the table above.");
        report.AppendLine();

        foreach (var outcome in failures)
        {
            report.AppendLine($"### `{outcome.Case.Id}` — {Escape(outcome.Case.Title)}");
            report.AppendLine();
            report.AppendLine($"*Tags:* {string.Join(", ", outcome.Case.Tags)} · " +
                              $"*Average score:* {outcome.AverageScore:0.0}/{summary.MaxScore}");
            report.AppendLine();

            foreach (var attempt in outcome.Attempts)
            {
                if (summary.Repeat > 1)
                {
                    report.AppendLine($"**Attempt {attempt.Attempt} of {summary.Repeat}** — " +
                                      $"{(attempt.Passed ? "pass" : "fail")}, {attempt.LatencyMs} ms");
                    report.AppendLine();
                }

                if (attempt.Error is not null)
                {
                    report.AppendLine($"- The call failed: {Escape(attempt.Error)}");
                }

                foreach (var dish in attempt.Dishes)
                {
                    report.AppendLine($"- **{dish.DishId}** `{dish.Status}` — {Escape(dish.Description)}");
                    report.AppendLine($"  - {Escape(dish.Explanation)}");
                }

                var failedChecks = attempt.Checks.Where(check => !check.Passed).ToList();
                if (failedChecks.Count > 0)
                {
                    report.AppendLine("- Failed code checks:");
                    foreach (var check in failedChecks)
                    {
                        report.AppendLine($"  - `{check.Name}`: {Escape(check.Detail)}");
                    }

                    // The untouched payload is only worth printing when its own shape was rejected.
                    if (failedChecks.Any(check => check.Name == "raw-payload-matches-contract")
                        && attempt.RawModelText is { Length: > 0 })
                    {
                        report.AppendLine($"  - raw model reply: `{Escape(Truncate(attempt.RawModelText, 400))}`");
                    }
                }

                if (attempt.Judge is not null)
                {
                    report.AppendLine($"- Judge scored {attempt.Score}/{summary.MaxScore} " +
                                      $"(needs {attempt.MinScore}) and said " +
                                      $"{(attempt.Judge.Passed ? "pass" : "fail")}:");

                    if (!attempt.JudgeArithmeticConsistent)
                    {
                        report.AppendLine($"  - The judge reported {attempt.JudgeReportedScore} but marked only " +
                                          $"{attempt.Score} criteria as met; the recounted score is used.");
                    }

                    foreach (var criterion in attempt.Judge.Criteria)
                    {
                        report.AppendLine($"  - {(criterion.Met ? "met" : "**not met**")} " +
                                          $"`{criterion.Name}`: {Escape(criterion.Note)}");
                    }

                    foreach (var reason in attempt.Judge.Reasons)
                    {
                        report.AppendLine($"  - reason: {Escape(reason)}");
                    }
                }
                else if (attempt.JudgeError is not null)
                {
                    report.AppendLine($"- The judge returned no verdict: {Escape(attempt.JudgeError)}");
                }

                report.AppendLine();
            }
        }
    }

    /// <summary>
    /// Builds the conclusion from the aggregated numbers rather than asking a model to write prose,
    /// so the conclusion cannot say anything the results do not support.
    /// </summary>
    private static string BuildConclusion(EvalRunSummary summary)
    {
        var lines = new List<string>();
        var tags = summary.TagBreakdown;

        var strong = tags.Where(tag => tag.PassRate >= 0.999).Select(tag => tag.Tag).ToList();
        var weak = tags.Where(tag => tag.PassRate < 0.999).OrderBy(tag => tag.PassRate).ToList();

        lines.Add(
            $"`{summary.EvaluatedModel}` passed {summary.PassedCases} of {summary.CaseCount} cases " +
            $"with an average score of {summary.AverageScore:0.00} out of {summary.MaxScore}.");

        if (strong.Count > 0)
        {
            lines.Add($"**Handled reliably:** {string.Join(", ", strong)} — every case carrying these tags passed.");
        }

        if (weak.Count > 0)
        {
            var detail = weak.Select(tag => $"{tag.Tag} ({tag.Passed}/{tag.Cases})");
            lines.Add($"**Weakest areas:** {string.Join(", ", detail)}.");

            var worstCases = summary.Cases
                .Where(outcome => !outcome.Passed)
                .Select(outcome => $"`{outcome.Case.Id}`")
                .ToList();
            lines.Add($"**Failing cases:** {string.Join(", ", worstCases)}.");
        }
        else
        {
            lines.Add("No case failed in this run.");
        }

        if (summary.TopFailedChecks.Count > 0)
        {
            lines.Add($"**Most frequent code check failures:** {string.Join(", ", summary.TopFailedChecks.Take(5))}.");
        }
        else
        {
            lines.Add("Every response satisfied the response contract, so the structural side of the feature is solid.");
        }

        if (summary.TopUnmetCriteria.Count > 0)
        {
            lines.Add($"**Most frequently unmet judge criteria:** {string.Join(", ", summary.TopUnmetCriteria.Take(5))}.");
        }

        if (summary.Repeat > 1)
        {
            var flaky = summary.Cases
                .Where(outcome => outcome.Attempts.Select(attempt => attempt.Passed).Distinct().Count() > 1)
                .Select(outcome => $"`{outcome.Case.Id}`")
                .ToList();

            lines.Add(flaky.Count > 0
                ? $"**Unstable across repeats:** {string.Join(", ", flaky)} passed on some attempts and failed on others."
                : "Every case gave the same verdict on all repeats, so the behaviour was stable in this run.");
        }

        return string.Join(Environment.NewLine + Environment.NewLine, lines);
    }

    private static string Truncate(string value, int limit) =>
        value.Length <= limit ? value : value[..limit] + "...";

    /// <summary>Keeps model text from breaking the Markdown table or the surrounding layout.</summary>
    private static string Escape(string? value) => (value ?? string.Empty)
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Trim();
}
