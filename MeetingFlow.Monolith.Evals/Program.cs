using System.ClientModel;
using System.Diagnostics;
using MeetingFlow.Monolith.Evals;
using MeetingFlow.Monolith.Models;
using MeetingFlow.Monolith.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

// Exit codes: 0 every case passed, 1 the run completed with failures, 2 the run could not start.
try
{
    return await EvalRunner.RunAsync(args);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("The run was cancelled.");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"The eval run could not start: {exception.Message}");
    return 2;
}

internal static class EvalRunner
{
    internal static async Task<int> RunAsync(string[] args)
    {
        var settings = EvalConfiguration.Build(args);

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            PrintMissingKeyGuide();
            return 2;
        }

        var caseFile = EvalCaseLoader.Load(settings.CasesFile);
        var selected = SelectCases(caseFile.Cases, settings);

        if (selected.Count == 0)
        {
            Console.Error.WriteLine("No case matched the --tag and --case filters.");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Console.WriteLine("Stopping after the current case...");
            cancellation.Cancel();
        };

        Console.WriteLine($"Evaluated model : {settings.Model} ({settings.Endpoint})");
        Console.WriteLine($"Judge model     : {settings.JudgeModel} ({settings.JudgeEndpoint})");

        if (settings.JudgeIsSameModel)
        {
            Console.WriteLine("Warning         : the judge is the same model as the system under test.");
            Console.WriteLine("                  Set Eval:JudgeModel to a stronger model for a more honest score.");
        }

        Console.WriteLine($"Cases           : {selected.Count} ({settings.Repeat} attempt(s) each)");
        Console.WriteLine($"Concurrency     : {settings.Concurrency}");
        Console.WriteLine();

        using var evaluatedClient = CreateChatClient(settings.ApiKey, settings.Endpoint, settings.Model);
        using var judgeClient = CreateChatClient(settings.JudgeApiKey, settings.JudgeEndpoint, settings.JudgeModel);
        var judge = new KosherJudge(judgeClient);

        var stopwatch = Stopwatch.StartNew();
        List<CaseOutcome> outcomes;

        try
        {
            outcomes = await EvaluateCasesAsync(
                selected, caseFile, settings, evaluatedClient, judge, cancellation.Token);
        }
        catch (EvalQuotaExhaustedException exception)
        {
            stopwatch.Stop();
            PrintQuotaGuide(exception.Message);
            return 2;
        }

        stopwatch.Stop();

        var summary = new EvalRunSummary
        {
            RunDate = DateTimeOffset.Now,
            EvaluatedModel = settings.Model,
            EvaluatedEndpoint = settings.Endpoint,
            JudgeModel = settings.JudgeModel,
            JudgeEndpoint = settings.JudgeEndpoint,
            JudgeIsSameModel = settings.JudgeIsSameModel,
            CasesFile = settings.CasesFileForDisplay,
            Repeat = settings.Repeat,
            MaxScore = caseFile.MaxScore,
            DurationMs = stopwatch.ElapsedMilliseconds,
            Cases = outcomes
        };

        var reportPath = ReportWriter.Write(summary, settings.ReportDirectory);

        Console.WriteLine();
        Console.WriteLine($"Passing cases : {summary.PassedCases} of {summary.CaseCount}");
        Console.WriteLine($"Average score : {summary.AverageScore:0.00} of {summary.MaxScore}");
        Console.WriteLine($"Report        : {reportPath}");

        return summary.FailedCases == 0 ? 0 : 1;
    }

    private static async Task<List<CaseOutcome>> EvaluateCasesAsync(
        List<EvalCase> cases,
        EvalCaseFile caseFile,
        EvalSettings settings,
        IChatClient evaluatedClient,
        KosherJudge judge,
        CancellationToken cancellationToken)
    {
        var outcomes = new CaseOutcome?[cases.Count];
        using var gate = new SemaphoreSlim(settings.Concurrency, settings.Concurrency);

        var work = cases.Select(async (evalCase, index) =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                outcomes[index] = await EvaluateCaseAsync(
                    evalCase, caseFile, settings, evaluatedClient, judge, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(work);
        return outcomes.Where(outcome => outcome is not null).Select(outcome => outcome!).ToList();
    }

    private static async Task<CaseOutcome> EvaluateCaseAsync(
        EvalCase evalCase,
        EvalCaseFile caseFile,
        EvalSettings settings,
        IChatClient evaluatedClient,
        KosherJudge judge,
        CancellationToken cancellationToken)
    {
        var minScore = evalCase.MinScore ?? caseFile.DefaultMinScore;
        var attempts = new List<AttemptOutcome>();

        for (var attempt = 1; attempt <= settings.Repeat; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outcome = await EvaluateAttemptAsync(
                evalCase, attempt, minScore, caseFile.MaxScore, settings, evaluatedClient, judge, cancellationToken);
            attempts.Add(outcome);

            var label = settings.Repeat > 1 ? $"{evalCase.Id} #{attempt}" : evalCase.Id;
            Console.WriteLine(
                $"  {(outcome.Passed ? "pass" : "FAIL")}  {label,-18} " +
                $"score {outcome.Score}/{caseFile.MaxScore}  " +
                $"{string.Join(",", outcome.Dishes.Select(dish => dish.Status))}" +
                $"{(outcome.FailedCheckNames.Count > 0 ? $"  [{string.Join(", ", outcome.FailedCheckNames)}]" : string.Empty)}" +
                $"{(outcome.Error is not null ? $"  error: {outcome.Error}" : string.Empty)}"
                + $"{(outcome.Judge is null && outcome.JudgeError is not null ? $"  JUDGE UNAVAILABLE: {outcome.JudgeError}" : string.Empty)}");

            if (settings.DelayMs > 0)
            {
                await Task.Delay(settings.DelayMs, cancellationToken);
            }
        }

        return new CaseOutcome { Case = evalCase, Attempts = attempts };
    }

    private static async Task<AttemptOutcome> EvaluateAttemptAsync(
        EvalCase evalCase,
        int attempt,
        int minScore,
        int maxScore,
        EvalSettings settings,
        IChatClient evaluatedClient,
        KosherJudge judge,
        CancellationToken cancellationToken)
    {
        // Mirrors exactly how KosherCheck.cshtml.cs builds its request, so the eval exercises the
        // same identifiers and the same batching the page uses.
        var entries = evalCase.Dishes
            .Select((dish, index) => new DishCheckEntry($"dish-{index + 1}", dish.Description))
            .ToList();

        var capture = new RawCaptureChatClient(evaluatedClient);

        // A fresh gate per attempt, so the service's own concurrency limit never rejects an eval call.
        var service = new OpenAiKosherAssessmentService(
            capture,
            NullLogger<OpenAiKosherAssessmentService>.Instance,
            new SemaphoreSlim(1, 1));

        DishAssessmentBatch? batch = null;
        string? error = null;
        var stopwatch = Stopwatch.StartNew();

        // A provider that is near its quota queues requests instead of rejecting them, which reads as
        // a hang. The timeout turns that into a reported failure the report can show.
        using var assessCall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        assessCall.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            batch = await Retry.RunAsync(
                token => service.AssessAsync(entries, token),
                settings.MaxRetries,
                assessCall.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            error = $"the provider did not answer within {settings.TimeoutSeconds}s";
        }
        catch (EvalQuotaExhaustedException)
        {
            throw;
        }
        catch (Exception exception)
        {
            error = Describe(exception);
        }

        stopwatch.Stop();

        var checks = DeterministicChecks.Run(evalCase, entries, batch, capture.LastRawText, error);

        var dishes = batch is null
            ? []
            : batch.Items.Select((item, index) => new DishOutcome
            {
                DishId = item.DishId,
                Description = entries.ElementAtOrDefault(index)?.Description ?? string.Empty,
                Status = KosherStatusNames.ToWire(item.Status),
                Explanation = item.Explanation
            }).ToList();

        JudgeVerdict? verdict = null;
        string? judgeError = null;

        if (dishes.Count > 0)
        {
            try
            {
                using var judgeCall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                judgeCall.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

                verdict = await Retry.RunAsync(
                    token => judge.JudgeAsync(evalCase, dishes, token),
                    settings.MaxRetries,
                    judgeCall.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                judgeError = $"the judge did not answer within {settings.TimeoutSeconds}s";
            }
            catch (EvalQuotaExhaustedException)
            {
                throw;
            }
            catch (Exception exception)
            {
                judgeError = Describe(exception);
            }
        }
        else
        {
            judgeError = "There was no response to grade.";
        }

        return new AttemptOutcome
        {
            Attempt = attempt,
            MinScore = minScore,
            MaxScore = maxScore,
            Checks = checks,
            Dishes = dishes,
            Judge = verdict,
            JudgeError = judgeError,
            Error = error,
            RawModelText = capture.LastRawText,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };
    }

    private static List<EvalCase> SelectCases(List<EvalCase> cases, EvalSettings settings)
    {
        IEnumerable<EvalCase> selected = cases;

        if (settings.CaseIds.Length > 0)
        {
            selected = selected.Where(evalCase =>
                settings.CaseIds.Contains(evalCase.Id, StringComparer.OrdinalIgnoreCase));
        }

        if (settings.Tags.Length > 0)
        {
            selected = selected.Where(evalCase =>
                evalCase.Tags.Any(tag => settings.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        return selected.ToList();
    }

    private static IChatClient CreateChatClient(string apiKey, string endpoint, string model)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient();
    }

    private static string Describe(Exception exception) => exception.InnerException is null
        ? exception.Message
        : $"{exception.Message} ({exception.InnerException.Message})";

    private static void PrintQuotaGuide(string providerMessage)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("The run stopped: the provider's per-day budget is used up.");
        Console.Error.WriteLine();
        Console.Error.WriteLine($"  {providerMessage}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("This limit is on the account, not on the key, so issuing a new API key does");
        Console.Error.WriteLine("not reset it. No report was written, because partial results would understate");
        Console.Error.WriteLine("the model rather than measure it.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  - wait for the provider's daily window to reset, then run once");
        Console.Error.WriteLine("  - run a subset now, for example --tag injection or --case persuade-003");
        Console.Error.WriteLine("  - point AiChat:Endpoint and AiChat:ApiKey at a provider with budget left");
        Console.Error.WriteLine("  - drop --judge-model so the judge reuses the evaluated model");
    }

    private static void PrintMissingKeyGuide()
    {
        Console.Error.WriteLine("No API key was found, so no model call can be made.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Set one of these, in order of precedence (lowest first):");
        Console.Error.WriteLine("  1. MeetingFlow.Monolith/appsettings.Local.json  ->  AiChat:ApiKey");
        Console.Error.WriteLine("     (the same file the web application already uses, and it is gitignored)");
        Console.Error.WriteLine("  2. environment variable                         ->  AiChat__ApiKey");
        Console.Error.WriteLine("  3. command line flag                            ->  --api-key <key>");
        Console.Error.WriteLine();
        Console.Error.WriteLine("See MeetingFlow.Monolith.Evals/README.md for the full list of settings.");
    }
}

/// <summary>
/// Raised when the provider's per-day budget is gone. This is not a finding about the model, and no
/// amount of waiting inside a run will clear it, so the run stops instead of grinding through retries.
/// </summary>
internal sealed class EvalQuotaExhaustedException(string message) : Exception(message);

/// <summary>
/// Retries a model call when the provider is rate limiting or briefly unavailable. Free tiers reject
/// bursts, and a rejected request is a provider problem, not a finding about the model's judgement.
/// </summary>
internal static class Retry
{
    internal static async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception exception) when (IsDailyBudgetGone(exception))
            {
                throw new EvalQuotaExhaustedException(Summarize(exception, 400));
            }
            catch (Exception exception) when (attempt < maxRetries
                && !cancellationToken.IsCancellationRequested
                && IsTransient(exception))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2);
                Console.WriteLine($"        provider rejected the call ({Summarize(exception)}), " +
                                  $"retrying in {delay.TotalSeconds:0}s");
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <summary>
    /// A per-day or per-month cap, as opposed to a per-minute burst limit. The provider names the
    /// window in the message, and a day-long window cannot be waited out inside a run.
    /// </summary>
    private static bool IsDailyBudgetGone(Exception exception) => Unwrap(exception).Any(inner =>
        inner.Message.Contains("per day", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("(TPD)", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("(RPD)", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("billing", StringComparison.OrdinalIgnoreCase));

    private static bool IsTransient(Exception exception) => Unwrap(exception).Any(inner =>
        inner is HttpRequestException ||
        inner is TimeoutException ||
        inner.Message.Contains("429", StringComparison.Ordinal) ||
        inner.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("overloaded", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("service unavailable", StringComparison.OrdinalIgnoreCase) ||
        inner.Message.Contains("503", StringComparison.Ordinal) ||
        inner.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Exception> Unwrap(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private static string Summarize(Exception exception, int limit = 120)
    {
        var message = Unwrap(exception).Last().Message;
        return message.Length <= limit ? message : message[..limit] + "...";
    }
}
