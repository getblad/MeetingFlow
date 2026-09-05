using System.ComponentModel;

namespace MeetingFlow.KosherEvals.Tests.Models;

public sealed class JudgeResult
{
    // 0: required clarification or decision is missing; 1: present without reasoning; 2: with reasoning.
    [Description("An integer score from 0 to 2 for the required clarification or decision and its reasoning, separate from invented facts and not the judge's confidence.")]
    public required int Score { get; init; }

    // Whether the answer invents facts about the dish that are not in the description.
    [Description("Whether the answer asserts unsupported facts about this specific dish, such as its ingredients, certification, or preparation. Evaluate independently of Score.")]
    public required bool HasInventedFacts { get; init; }

    // A short explanation of the score and the invented-facts check.
    [Description("A short explanation in English of the score and the invented-facts check, identifying any unsupported claim found.")]
    public required string Reason { get; init; }
}
