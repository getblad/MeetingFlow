using System.Text.Json.Serialization;

namespace MeetingFlow.Monolith.Evals;

/// <summary>What the evaluated system said about one dish.</summary>
public sealed class DishOutcome
{
    public required string DishId { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public required string Explanation { get; init; }
}

/// <summary>The result of one deterministic, code-only check.</summary>
public sealed class CheckOutcome
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public required string Detail { get; init; }
}

/// <summary>One run of one case. With --repeat there is more than one attempt per case.</summary>
public sealed class AttemptOutcome
{
    public required int Attempt { get; init; }
    public required int MinScore { get; init; }
    public required int MaxScore { get; init; }
    public required List<CheckOutcome> Checks { get; init; }
    public List<DishOutcome> Dishes { get; init; } = [];
    public JudgeVerdict? Judge { get; init; }
    public string? JudgeError { get; init; }
    public string? Error { get; init; }
    public string? RawModelText { get; init; }
    public long LatencyMs { get; init; }

    public bool DeterministicPassed => Checks.All(check => check.Passed);

    public List<string> FailedCheckNames =>
        Checks.Where(check => !check.Passed).Select(check => check.Name).ToList();

    /// <summary>
    /// The authoritative score, recounted in code from the judge's own criteria rather than
    /// trusting the number the judge wrote. A judge that cannot add up does not get to decide the score.
    /// </summary>
    public int Score => Judge?.Criteria.Count(criterion => criterion.Met) ?? 0;

    public int? JudgeReportedScore => Judge?.Score;

    public bool JudgeArithmeticConsistent => Judge is null || Judge.Score == Score;

    /// <summary>An attempt passes only when the code checks pass and the judge score clears the bar.</summary>
    public bool Passed => DeterministicPassed && Judge is not null && Score >= MinScore;
}

public sealed class CaseOutcome
{
    public required EvalCase Case { get; init; }
    public required List<AttemptOutcome> Attempts { get; init; }

    /// <summary>Strict on purpose: with repeats, every attempt must pass, so a flaky case is a failing case.</summary>
    public bool Passed => Attempts.Count > 0 && Attempts.All(attempt => attempt.Passed);

    public double AverageScore => Attempts.Count == 0 ? 0 : Attempts.Average(attempt => attempt.Score);

    [JsonIgnore]
    public IEnumerable<string> Tags => Case.Tags;
}

public sealed class TagSummary
{
    public required string Tag { get; init; }
    public required int Cases { get; init; }
    public required int Passed { get; init; }
    public required double AverageScore { get; init; }
    public double PassRate => Cases == 0 ? 0 : (double)Passed / Cases;
}

public sealed class EvalRunSummary
{
    public required DateTimeOffset RunDate { get; init; }
    public required string EvaluatedModel { get; init; }
    public required string EvaluatedEndpoint { get; init; }
    public required string JudgeModel { get; init; }
    public required string JudgeEndpoint { get; init; }
    public required bool JudgeIsSameModel { get; init; }
    public required string CasesFile { get; init; }
    public required int Repeat { get; init; }
    public required int MaxScore { get; init; }
    public required long DurationMs { get; init; }
    public required List<CaseOutcome> Cases { get; init; }

    public int CaseCount => Cases.Count;
    public int PassedCases => Cases.Count(outcome => outcome.Passed);
    public int FailedCases => CaseCount - PassedCases;
    public int AttemptCount => Cases.Sum(outcome => outcome.Attempts.Count);

    public double AverageScore => AttemptCount == 0
        ? 0
        : Cases.SelectMany(outcome => outcome.Attempts).Average(attempt => attempt.Score);

    public int DeterministicFailures =>
        Cases.SelectMany(outcome => outcome.Attempts).Count(attempt => !attempt.DeterministicPassed);

    public int JudgeFailures => Cases
        .SelectMany(outcome => outcome.Attempts)
        .Count(attempt => attempt.DeterministicPassed && !attempt.Passed);

    public List<TagSummary> TagBreakdown => Cases
        .SelectMany(outcome => outcome.Case.Tags.Select(tag => (tag, outcome)))
        .GroupBy(pair => pair.tag, StringComparer.OrdinalIgnoreCase)
        .Select(group => new TagSummary
        {
            Tag = group.Key,
            Cases = group.Count(),
            Passed = group.Count(pair => pair.outcome.Passed),
            AverageScore = group.Average(pair => pair.outcome.AverageScore)
        })
        .OrderBy(summary => summary.PassRate)
        .ThenBy(summary => summary.Tag, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>Most frequently failed code check names, used to write the conclusion.</summary>
    public List<string> TopFailedChecks => Cases
        .SelectMany(outcome => outcome.Attempts)
        .SelectMany(attempt => attempt.FailedCheckNames)
        .GroupBy(name => name, StringComparer.Ordinal)
        .OrderByDescending(group => group.Count())
        .Select(group => $"{group.Key} ({group.Count()}x)")
        .ToList();

    /// <summary>Most frequently unmet judge criteria, used to write the conclusion.</summary>
    public List<string> TopUnmetCriteria => Cases
        .SelectMany(outcome => outcome.Attempts)
        .Where(attempt => attempt.Judge is not null)
        .SelectMany(attempt => attempt.Judge!.Criteria.Where(criterion => !criterion.Met))
        .GroupBy(criterion => criterion.Name, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .Select(group => $"{group.Key} ({group.Count()}x)")
        .ToList();
}
