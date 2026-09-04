using System.Text.Json;
using MeetingFlow.Monolith.Models;
using MeetingFlow.Monolith.Services;

namespace MeetingFlow.Monolith.Evals;

/// <summary>
/// The wire spellings of <see cref="DishAssessmentStatus"/>, which is what the page returns
/// to the browser and therefore what the case files are written against.
/// </summary>
public static class KosherStatusNames
{
    public const string Kosher = "KOSHER";
    public const string NotKosher = "NOT_KOSHER";
    public const string Conditional = "CONDITIONAL";
    public const string InvalidInput = "INVALID_INPUT";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Kosher, NotKosher, Conditional, InvalidInput };

    public static string ToWire(DishAssessmentStatus status) => status switch
    {
        DishAssessmentStatus.Kosher => Kosher,
        DishAssessmentStatus.NotKosher => NotKosher,
        DishAssessmentStatus.Conditional => Conditional,
        DishAssessmentStatus.InvalidInput => InvalidInput,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown kosher assessment status.")
    };
}

/// <summary>One dish inside a case, with the statuses this dish is and is not allowed to receive.</summary>
public sealed class EvalDish
{
    public required string Description { get; init; }

    /// <summary>Statuses that count as acceptable. Empty means any status in the contract is accepted.</summary>
    public List<string> AllowedStatuses { get; init; } = [];

    /// <summary>Statuses that are always wrong for this dish. This is usually the sharper assertion.</summary>
    public List<string> ForbiddenStatuses { get; init; } = [];

    /// <summary>Substrings the explanation must not contain, compared case-insensitively.</summary>
    public List<string> MustNotContain { get; init; } = [];

    public int MinExplanationLength { get; init; } = 20;
}

public sealed class EvalCase
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public List<string> Tags { get; init; } = [];

    /// <summary>Plain-language criteria handed to the language-model judge for this case.</summary>
    public required string Rubric { get; init; }

    /// <summary>Judge score needed to pass. Falls back to the file-level default.</summary>
    public int? MinScore { get; init; }

    public List<EvalDish> Dishes { get; init; } = [];
}

public sealed class EvalCaseFile
{
    public int MaxScore { get; init; } = 5;
    public int DefaultMinScore { get; init; } = 4;
    public List<EvalCase> Cases { get; init; } = [];
}

public static class EvalCaseLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static EvalCaseFile Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The eval case file '{path}' was not found.", path);
        }

        var file = JsonSerializer.Deserialize<EvalCaseFile>(File.ReadAllText(path), SerializerOptions)
            ?? throw new InvalidOperationException($"The eval case file '{path}' could not be read.");

        Validate(file, path);
        return file;
    }

    /// <summary>
    /// Fails fast on a badly written case file, so a typo in a case never shows up later as a
    /// model failure. This is itself a deterministic check, just one that runs before any model call.
    /// </summary>
    private static void Validate(EvalCaseFile file, string path)
    {
        var problems = new List<string>();

        if (file.Cases.Count == 0)
        {
            problems.Add("the file contains no cases");
        }

        if (file.MaxScore < 1)
        {
            problems.Add("maxScore must be at least 1");
        }

        foreach (var duplicate in file.Cases
            .GroupBy(evalCase => evalCase.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            problems.Add($"case id '{duplicate.Key}' is used {duplicate.Count()} times");
        }

        foreach (var evalCase in file.Cases)
        {
            if (string.IsNullOrWhiteSpace(evalCase.Rubric))
            {
                problems.Add($"{evalCase.Id}: rubric is empty");
            }

            if (evalCase.Dishes.Count is < KosherInputValidatorLimits.MinimumDishCount
                or > KosherInputValidatorLimits.MaximumDishCount)
            {
                problems.Add($"{evalCase.Id}: a case must hold between 1 and 10 dishes, the page rejects anything else");
            }

            foreach (var dish in evalCase.Dishes)
            {
                if (string.IsNullOrWhiteSpace(dish.Description))
                {
                    problems.Add($"{evalCase.Id}: a dish description is empty");
                }
                else if (dish.Description.Length > KosherInputValidatorLimits.MaximumDishLength)
                {
                    problems.Add($"{evalCase.Id}: a dish description is longer than 500 characters, the page rejects it");
                }

                foreach (var status in dish.AllowedStatuses.Concat(dish.ForbiddenStatuses))
                {
                    if (!KosherStatusNames.All.Contains(status))
                    {
                        problems.Add($"{evalCase.Id}: '{status}' is not one of {string.Join(", ", KosherStatusNames.All)}");
                    }
                }

                var contradiction = dish.AllowedStatuses.Intersect(dish.ForbiddenStatuses, StringComparer.Ordinal).ToList();
                if (contradiction.Count > 0)
                {
                    problems.Add($"{evalCase.Id}: '{string.Join(", ", contradiction)}' is both allowed and forbidden");
                }
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"The eval case file '{path}' is not valid:{Environment.NewLine}  - " +
                string.Join($"{Environment.NewLine}  - ", problems));
        }
    }
}

/// <summary>Mirrors the input limits the page enforces, so cases can never ask for input the page would reject.</summary>
internal static class KosherInputValidatorLimits
{
    internal const int MinimumDishCount = KosherInputValidator.MinimumDishCount;
    internal const int MaximumDishCount = KosherInputValidator.MaximumDishCount;
    internal const int MaximumDishLength = KosherInputValidator.MaximumDishLength;
}
