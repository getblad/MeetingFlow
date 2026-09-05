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
    [Fact]
    public async Task Kosher_flow_passes_eval_cases()
    {
        using var setup = new KosherTestSetup();
        var service = setup.Service;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var startedAt = DateTimeOffset.UtcNow;

        var cases = KosherTestData.All;

        var evaluations = new List<CaseEvaluation>();
        string? error = null;
        var stage = "service";
        try
        {
            foreach (var testCase in cases)
            {
                // Send each case separately, without expectations or other dishes.
                stage = $"service: {testCase.Id}";
                var dish = new DishCheckEntry(testCase.Id, testCase.Dish);
                var result = await service.AssessAsync([dish], timeout.Token);
                var actual = Assert.Single(result.Items, item => item.DishId == testCase.Id);

                stage = $"judge: {testCase.Id}";
                var judgment = await setup.Judge.EvaluateAsync(testCase, actual.Explanation, timeout.Token);

                // Explicitly show what is checked and when the case passes.
                var codePassed = actual.Status == testCase.ExpectedStatus;
                var judgePassed = judgment.Score == 2 && !judgment.HasInventedFacts;

                evaluations.Add(new CaseEvaluation
                {
                    Case = testCase,
                    Actual = actual,
                    Judgment = judgment,
                    CodePassed = codePassed,
                    JudgePassed = judgePassed,
                    Passed = codePassed && judgePassed
                });
            }
        }
        catch (Exception exception)
        {
            // Do not turn an execution error into score 0 or copy raw exception messages into reports.
            error = $"Stage '{stage}' failed ({exception.GetType().Name}).";
            throw;
        }
        finally
        {
            // Save JSON and HTML before assertions, including incomplete runs after an error.
            var report = new EvalReport(
                StartedAtUtc: startedAt,
                Model: setup.Model,
                JudgeModel: setup.JudgeModel,
                TotalCases: cases.Length,
                Cases: evaluations,
                Error: error);

            var reportsDirectory = Path.Combine(AppContext.BaseDirectory, "reports");
            var reportFiles = await ReportWriter.SaveJsonAndHtmlAsync(report, reportsDirectory);

            output.WriteLine($"JSON: {reportFiles.JsonPath}");
            output.WriteLine($"HTML: {reportFiles.HtmlPath}");
        }

        Assert.All(evaluations, evaluation =>
        {
            // Assert the saved results without repeating the comparison rules.
            Assert.True(evaluation.CodePassed,
                $"{evaluation.Case.Id}: expected {evaluation.Case.ExpectedStatus}, actual {evaluation.Actual.Status}.");
            Assert.True(evaluation.JudgePassed,
                $"{evaluation.Case.Id}: Score = {evaluation.Judgment.Score}, " +
                $"HasInventedFacts = {evaluation.Judgment.HasInventedFacts}. {evaluation.Judgment.Reason}");
        });
    }
}
