using MeetingFlow.KosherEvals.Tests.Models;
using MeetingFlow.Monolith.Models;
using Xunit;
using Xunit.Abstractions;

namespace MeetingFlow.KosherEvals.Tests;

public sealed class KosherEvalTests(ITestOutputHelper output)
{
    // Goal: verify that the model identifies what information needs clarification
    // and explains why it matters for assessing whether a dish is kosher.
    // When the information is sufficient, it should explain the decision without
    // unnecessary clarification. It must not invent facts about the dish.
    // Each explanation must contain no more than 1000 characters.
    // [EvalFact]
    [Fact]
    public async Task Kosher_flow_passes_eval_cases()
    {
        const int maximumExplanationLength = 1000;
        using var setup = new KosherTestSetup();
        var service = setup.Service;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var startedAt = DateTimeOffset.UtcNow;

        var cases = new KosherTestData().Cases;

        var evaluations = new List<CaseEvaluation>();
        foreach (var testCase in cases)
        {
            DishAssessmentItem? actual = null;

            try
            {
                // Send each case separately, without expectations or other dishes.
                var dish = new DishCheckEntry(testCase.Id, testCase.Dish);
                var result = await service.AssessAsync([dish], timeout.Token);
                actual = Assert.Single(result.Items, item => item.DishId == testCase.Id);

                var judgment = await setup.Judge.EvaluateAsync(testCase, actual.Explanation, timeout.Token);

                // Explicitly show what is checked and when the case passes.
                var codePassed = actual.Status == testCase.ExpectedStatus;
                var explanationLength = actual.Explanation.Length;
                var lengthPassed = explanationLength <= maximumExplanationLength;
                var judgePassed = judgment.Score == 2 && !judgment.HasInventedFacts;

                evaluations.Add(new CaseEvaluation
                {
                    Case = testCase,
                    Actual = actual,
                    Judgment = judgment,
                    CodePassed = codePassed,
                    ExplanationLength = explanationLength,
                    LengthPassed = lengthPassed,
                    JudgePassed = judgePassed,
                    Passed = codePassed && lengthPassed && judgePassed
                });
            }
            catch (Exception exception)
            {
                // Keep the failed case in the report and continue with the remaining cases.
                var failedStep = actual is null ? "Service" : "Judge";
                var codePassed = actual is not null && actual.Status == testCase.ExpectedStatus;
                var explanationLength = actual?.Explanation.Length;
                var lengthPassed = explanationLength is not null && explanationLength <= maximumExplanationLength;

                evaluations.Add(new CaseEvaluation
                {
                    Case = testCase,
                    Actual = actual,
                    Judgment = null,
                    CodePassed = codePassed,
                    ExplanationLength = explanationLength,
                    LengthPassed = lengthPassed,
                    JudgePassed = false,
                    Passed = false,
                    Error = $"{failedStep} failed ({exception.GetType().Name}): {exception.Message}"
                });
            }
        }

        // Save JSON and HTML before assertions, so failed cases remain available for investigation.
        var report = new EvalReport(
            StartedAtUtc: startedAt,
            Model: setup.Model,
            JudgeModel: setup.JudgeModel,
            TotalCases: cases.Length,
            MaximumExplanationLength: maximumExplanationLength,
            Cases: evaluations);

        var reportsDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "reports"));
        var reportFiles = await ReportWriter.SaveJsonAndHtmlAsync(report, reportsDirectory);

        output.WriteLine($"JSON: {reportFiles.JsonPath}");
        output.WriteLine($"HTML: {reportFiles.HtmlPath}");

        Assert.All(evaluations, evaluation =>
        {
            Assert.True(evaluation.Error is null,
                $"{evaluation.Case.Id}: {evaluation.Error}");

            var actual = Assert.IsType<DishAssessmentItem>(evaluation.Actual);
            var judgment = Assert.IsType<JudgeResult>(evaluation.Judgment);

            // Assert the saved results without repeating the comparison rules.
            Assert.True(evaluation.CodePassed,
                $"{evaluation.Case.Id}: expected {evaluation.Case.ExpectedStatus}, actual {actual.Status}.");
            Assert.True(evaluation.LengthPassed,
                $"{evaluation.Case.Id}: explanation has {evaluation.ExplanationLength} characters; " +
                $"maximum is {maximumExplanationLength}.");
            Assert.True(evaluation.JudgePassed,
                $"{evaluation.Case.Id}: Score = {judgment.Score}, " +
                $"HasInventedFacts = {judgment.HasInventedFacts}. " +
                $"Score reasoning: {judgment.ScoreReasoning} " +
                $"Invented facts reasoning: {judgment.InventedFactsReasoning}");
        });
    }
}
