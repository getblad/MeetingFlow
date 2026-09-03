using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Evals;

public sealed record DeterministicCheckResult(bool Passed, IReadOnlyList<string> FailureReasons);

public static class DeterministicChecks
{
    private const int MaximumExplanationLength = 1_000;

    public static DeterministicCheckResult Run(EvalCase evalCase, DishAssessmentBatch response)
    {
        var failures = new List<string>();

        if (response.Items.Count != evalCase.Dishes.Count)
        {
            failures.Add($"Expected {evalCase.Dishes.Count} result(s), got {response.Items.Count}.");
        }

        for (var index = 0; index < response.Items.Count; index++)
        {
            var item = response.Items[index];

            if (!Enum.IsDefined(item.Status))
            {
                failures.Add($"Result {index + 1} has an undefined status.");
            }

            if (string.IsNullOrWhiteSpace(item.Explanation))
            {
                failures.Add($"Result {index + 1} has an empty explanation.");
            }
            else if (item.Explanation.Length > MaximumExplanationLength)
            {
                failures.Add($"Result {index + 1} explanation exceeds {MaximumExplanationLength} characters.");
            }
        }

        return new DeterministicCheckResult(failures.Count == 0, failures);
    }
}
