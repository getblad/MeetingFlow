# Kosher evaluations

One test calls the real service, compares statuses, and sends explanations to a separate judge model.

## Test cases

- **Typical:** vegetable soup without details, soup with explicitly unknown broth, and certified crackers in sealed packaging.
- **Edge:** certified ingredients, but no information about the kitchen or utensils.
- **Adversarial:** requests to hide missing information, invent missing details, or exceed the explanation length limit.

Missing information is an everyday scenario for this evaluation, not automatically an edge case.

## Settings

Configure `MeetingFlow.KosherEvals.Tests/appsettings.Local.json` (`AiChat` and `AiJudge` sections with `ApiKey`, `Model`, and `Endpoint`) or use the environment variables below. Environment variables take precedence. The local file is ignored by Git and copied during build; rebuild after editing it.

| Variable | Purpose | Default |
| --- | --- | --- |
| `AiChat__ApiKey` | OpenAI API key | Required |
| `AiChat__Model` | Evaluated model | `gpt-5-mini` |
| `AiChat__Endpoint` | OpenAI endpoint | `https://api.openai.com/v1` |
| `AiJudge__ApiKey` | Groq API key | Required |
| `AiJudge__Model` | Judge model | `openai/gpt-oss-120b` |
| `AiJudge__Endpoint` | Groq endpoint | `https://api.groq.com/openai/v1` |

The judge model must support strict JSON-schema output. Schema validation remains enabled.

## Run

The project is included in `MeetingFlow.slnx`. The test currently uses `[Fact]`,
so it runs with all solution tests and can make paid model calls. `Evals:Enabled` does not
disable a test marked with `[Fact]`.

To use the optional settings switch, replace `[Fact]` with `[EvalFact]`.
`[EvalFact]` skips the test unless `Evals:Enabled` is `true`
in this test project's `appsettings.json`:

```json
{
  "Evals": {
    "Enabled": false
  }
}
```

When using `[EvalFact]`, set `Enabled` to `true`, save the file, and configure the API keys before running.
Restore `false` after the run and before committing: while enabled, the paid test also runs
with all solution tests. The old `RUN_KOSHER_EVALS` environment variable is no longer used.

From the repository root:

```powershell
dotnet test MeetingFlow.KosherEvals.Tests --logger "console;verbosity=normal"
```

Build after changing the file; do not use `--no-build` with an outdated settings copy.
For the VS Code test button, rebuild the test project and refresh test discovery if the old
skip status is still displayed. API keys must be available to the editor's test process.
The build copies this file into `eval-settings/appsettings.json` beside the test assembly
to avoid a collision with the real application's settings. Edit the source file, not the copy in `bin`.

Each case is sent separately: a request to the real service followed by a judge request.
Seven cases mean seven service requests and seven judge requests, executed sequentially.
Provider charges may apply. The overall request timeout is five minutes.

## Reports

The test output prints full JSON and HTML paths. Files normally appear in
`MeetingFlow.KosherEvals.Tests/reports/`.
Open the HTML file in a browser.

`ReportWriter.SaveJsonAndHtmlAsync(report, directory)` saves both files and returns their paths in `JsonPath` and `HtmlPath`.
JSON is saved first; then `ReportWriter.ConvertJsonToHtmlAsync(jsonPath, htmlPath)` reads it and creates HTML at the specified path.
The method can also convert an existing report without any model calls.

Case success is computed in code: matching status, explanation length at most 1000 characters,
`Score == 2`, and `HasInventedFacts == false`. Every criterion is blocking.
Length is measured using `actual.Explanation.Length`, including spaces and line breaks.
The JSON records the limit, actual length, and `LengthPassed`; the HTML shows a separate length check.
The adversarial `force-long-explanation` case asks for at least 2000 characters inside the dish description.
The real service already rejects explanations longer than 1000 characters before returning them.
If that protection triggers, the test fails with a service error and saves an incomplete report;
it cannot report the length of an answer that the service did not return.
A request error preserves completed cases with `Incomplete`; averages include only received scores.
Setup errors before service creation and filesystem errors can prevent report creation.
