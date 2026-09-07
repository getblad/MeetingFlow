using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MeetingFlow.KosherEvals.Tests.Models;

public sealed class JudgeResult
{
    // Explain the score before returning its numeric value.
    [JsonPropertyOrder(0)]
    [Description("A short explanation in English of which scoring criteria the answer meets or misses. Cite the relevant words from the answer when they are present.")]
    public required string ScoreReasoning { get; init; }

    // 0: required clarification or decision is missing; 1: present without reasoning; 2: with reasoning.
    [JsonPropertyOrder(1)]
    [Description("An integer score from 0 to 2 for the required clarification or decision and its reasoning, separate from invented facts and not the judge's confidence.")]
    public required int Score { get; init; }

    // Explain the invented-facts check before returning its boolean value.
    [JsonPropertyOrder(2)]
    [Description("A short explanation in English of whether every dish-specific claim is supported. Identify any unsupported claim found.")]
    public required string InventedFactsReasoning { get; init; }

    // Whether the answer invents facts about the dish that are not in the description.
    [JsonPropertyOrder(3)]
    [Description("Whether the answer asserts unsupported facts about this specific dish, such as its ingredients, certification, or preparation. Evaluate independently of Score.")]
    public required bool HasInventedFacts { get; init; }
}
