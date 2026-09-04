using System.Text.Json;
using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Evals;

/// <summary>
/// Code-only checks. No language model is involved, so these are fully repeatable: given the same
/// model output they always produce the same verdict. They cover the response contract (shape,
/// identifiers, statuses, explanation limits) and the per-dish status expectations from the case file.
/// </summary>
public static class DeterministicChecks
{
    /// <summary>Matches the limit enforced inside OpenAiKosherAssessmentService.</summary>
    public const int MaximumExplanationLength = 1_000;

    public static List<CheckOutcome> Run(
        EvalCase evalCase,
        IReadOnlyList<DishCheckEntry> entries,
        DishAssessmentBatch? batch,
        string? rawModelText,
        string? error)
    {
        var checks = new List<CheckOutcome>
        {
            Check(
                "call-succeeded",
                error is null,
                error ?? "The assessment call completed without an exception."),
            CheckRawPayload(entries, rawModelText)
        };

        if (batch is null)
        {
            checks.Add(Check(
                "typed-result-available",
                false,
                "No validated result was produced, so the remaining checks could not run."));
            return checks;
        }

        var items = batch.Items;

        checks.Add(Check(
            "one-result-per-dish",
            items.Count == entries.Count,
            $"Requested {entries.Count} dishes and received {items.Count} assessments."));

        if (items.Count != entries.Count)
        {
            return checks;
        }

        var expectedIds = entries.Select(entry => entry.Id).ToList();
        var returnedIds = items.Select(item => item.DishId).ToList();

        checks.Add(Check(
            "dish-ids-preserved-in-order",
            expectedIds.SequenceEqual(returnedIds, StringComparer.Ordinal),
            $"Expected [{string.Join(", ", expectedIds)}] and received [{string.Join(", ", returnedIds)}]."));

        checks.Add(CheckStatusesInContract(items));
        checks.Add(CheckExplanationLengths(evalCase, items));
        checks.Add(CheckExplanationsAreEnglish(items));
        checks.Add(CheckAllowedStatuses(evalCase, entries, items));
        checks.Add(CheckForbiddenStatuses(evalCase, entries, items));
        checks.Add(CheckForbiddenText(evalCase, entries, items));

        return checks;
    }

    /// <summary>
    /// Validates the untouched model payload. The service already rejects a malformed reply by throwing,
    /// so this check exists to say what was wrong with it rather than only that something was.
    /// </summary>
    private static CheckOutcome CheckRawPayload(IReadOnlyList<DishCheckEntry> entries, string? rawModelText)
    {
        if (string.IsNullOrWhiteSpace(rawModelText))
        {
            return Check("raw-payload-matches-contract", false, "The model returned no text at all.");
        }

        try
        {
            using var document = JsonDocument.Parse(rawModelText);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Check("raw-payload-matches-contract", false, $"The payload root is {root.ValueKind}, not a JSON object.");
            }

            if (!root.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return Check("raw-payload-matches-contract", false, "The payload has no 'items' array.");
            }

            var problems = new List<string>();
            var index = 0;

            foreach (var item in itemsElement.EnumerateArray())
            {
                index++;

                if (!item.TryGetProperty("dishId", out var id) || id.ValueKind != JsonValueKind.String)
                {
                    problems.Add($"item {index} has no string 'dishId'");
                }

                if (!item.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String)
                {
                    problems.Add($"item {index} has no string 'status'");
                }
                else if (!KosherStatusNames.All.Contains(status.GetString() ?? string.Empty))
                {
                    problems.Add($"item {index} has status '{status.GetString()}', which is outside the contract");
                }

                if (!item.TryGetProperty("explanation", out var explanation) || explanation.ValueKind != JsonValueKind.String)
                {
                    problems.Add($"item {index} has no string 'explanation'");
                }
            }

            if (index != entries.Count)
            {
                problems.Add($"the payload holds {index} items for {entries.Count} dishes");
            }

            return Check(
                "raw-payload-matches-contract",
                problems.Count == 0,
                problems.Count == 0
                    ? $"The raw payload is a JSON object with {index} well-formed items."
                    : string.Join("; ", problems));
        }
        catch (JsonException exception)
        {
            return Check("raw-payload-matches-contract", false, $"The payload is not valid JSON: {exception.Message}");
        }
    }

    private static CheckOutcome CheckStatusesInContract(IReadOnlyList<DishAssessmentItem> items)
    {
        var offenders = items
            .Where(item => !Enum.IsDefined(item.Status))
            .Select(item => $"{item.DishId}={(int)item.Status}")
            .ToList();

        return Check(
            "status-in-contract",
            offenders.Count == 0,
            offenders.Count == 0
                ? $"Every status is one of {string.Join(", ", KosherStatusNames.All)}."
                : $"Statuses outside the contract: {string.Join(", ", offenders)}.");
    }

    private static CheckOutcome CheckExplanationLengths(EvalCase evalCase, IReadOnlyList<DishAssessmentItem> items)
    {
        var problems = new List<string>();

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var explanation = item.Explanation ?? string.Empty;
            var minimum = evalCase.Dishes.ElementAtOrDefault(index)?.MinExplanationLength ?? 20;

            if (string.IsNullOrWhiteSpace(explanation))
            {
                problems.Add($"{item.DishId} has an empty explanation");
            }
            else if (explanation.Trim().Length < minimum)
            {
                problems.Add($"{item.DishId} has a {explanation.Trim().Length} character explanation, below the {minimum} character minimum");
            }
            else if (explanation.Length > MaximumExplanationLength)
            {
                problems.Add($"{item.DishId} has a {explanation.Length} character explanation, above the {MaximumExplanationLength} character limit");
            }
        }

        return Check(
            "explanation-within-limits",
            problems.Count == 0,
            problems.Count == 0
                ? "Every explanation is present and within the length limits."
                : string.Join("; ", problems));
    }

    /// <summary>
    /// The system prompt promises an English explanation. A cheap script test catches a model that
    /// answers in the language of the dish description instead.
    /// </summary>
    private static CheckOutcome CheckExplanationsAreEnglish(IReadOnlyList<DishAssessmentItem> items)
    {
        var offenders = items
            .Where(item => (item.Explanation ?? string.Empty).Any(IsNonLatinScript))
            .Select(item => item.DishId)
            .ToList();

        return Check(
            "explanation-is-english",
            offenders.Count == 0,
            offenders.Count == 0
                ? "No explanation contains non-Latin script characters."
                : $"Non-Latin script found in: {string.Join(", ", offenders)}.");
    }

    private static bool IsNonLatinScript(char character)
    {
        var code = (int)character;
        return code is >= 0x0400 and <= 0x04FF   // Cyrillic
            or >= 0x0590 and <= 0x05FF           // Hebrew
            or >= 0x0600 and <= 0x06FF           // Arabic
            or >= 0x3040 and <= 0x30FF           // Hiragana and Katakana
            or >= 0x4E00 and <= 0x9FFF;          // CJK
    }

    private static CheckOutcome CheckAllowedStatuses(
        EvalCase evalCase,
        IReadOnlyList<DishCheckEntry> entries,
        IReadOnlyList<DishAssessmentItem> items)
    {
        var problems = new List<string>();

        for (var index = 0; index < items.Count; index++)
        {
            var allowed = evalCase.Dishes.ElementAtOrDefault(index)?.AllowedStatuses ?? [];
            if (allowed.Count == 0)
            {
                continue;
            }

            var actual = KosherStatusNames.ToWire(items[index].Status);
            if (!allowed.Contains(actual, StringComparer.Ordinal))
            {
                problems.Add($"{entries[index].Id} returned {actual}, expected one of {string.Join("/", allowed)}");
            }
        }

        return Check(
            "status-is-allowed",
            problems.Count == 0,
            problems.Count == 0
                ? "Every status is inside the set the case allows."
                : string.Join("; ", problems));
    }

    private static CheckOutcome CheckForbiddenStatuses(
        EvalCase evalCase,
        IReadOnlyList<DishCheckEntry> entries,
        IReadOnlyList<DishAssessmentItem> items)
    {
        var problems = new List<string>();

        for (var index = 0; index < items.Count; index++)
        {
            var forbidden = evalCase.Dishes.ElementAtOrDefault(index)?.ForbiddenStatuses ?? [];
            if (forbidden.Count == 0)
            {
                continue;
            }

            var actual = KosherStatusNames.ToWire(items[index].Status);
            if (forbidden.Contains(actual, StringComparer.Ordinal))
            {
                problems.Add($"{entries[index].Id} returned the forbidden status {actual}");
            }
        }

        return Check(
            "status-is-not-forbidden",
            problems.Count == 0,
            problems.Count == 0
                ? "No dish received a status the case rules out."
                : string.Join("; ", problems));
    }

    private static CheckOutcome CheckForbiddenText(
        EvalCase evalCase,
        IReadOnlyList<DishCheckEntry> entries,
        IReadOnlyList<DishAssessmentItem> items)
    {
        var problems = new List<string>();

        for (var index = 0; index < items.Count; index++)
        {
            var forbiddenText = evalCase.Dishes.ElementAtOrDefault(index)?.MustNotContain ?? [];
            var explanation = items[index].Explanation ?? string.Empty;

            foreach (var fragment in forbiddenText.Where(fragment =>
                explanation.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add($"{entries[index].Id} contains the forbidden text '{fragment}'");
            }
        }

        return Check(
            "explanation-avoids-forbidden-text",
            problems.Count == 0,
            problems.Count == 0
                ? "No explanation contains text the case rules out."
                : string.Join("; ", problems));
    }

    private static CheckOutcome Check(string name, bool passed, string detail) =>
        new() { Name = name, Passed = passed, Detail = detail };
}
