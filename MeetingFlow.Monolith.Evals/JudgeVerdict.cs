using System.ComponentModel;

namespace MeetingFlow.Monolith.Evals;

public sealed class JudgeVerdict
{
    [Description("The caseId being judged, copied exactly from the input.")]
    public required string CaseId { get; init; }

    [Description("A score from 0 to 5 reflecting how well the response satisfied the rubric.")]
    public required int Score { get; init; }

    [Description("The maximum possible score. Always 5.")]
    public required int MaxScore { get; init; }

    [Description("True when the response meets the minimum bar described in the rubric.")]
    public required bool Passed { get; init; }

    [Description("Short, specific reasons supporting the score, referencing what the response did or failed to do.")]
    public required List<string> Reasons { get; init; }
}
