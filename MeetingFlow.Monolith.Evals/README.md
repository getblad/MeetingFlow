# Kosher Check Evals

Automated evals for the `/KosherCheck` feature in `MeetingFlow.Monolith`. Calls the real
`OpenAiKosherAssessmentService` directly (no HTTP, no browser) for every case in `cases/`,
runs a deterministic format check, sends the result to a judge model, and writes a report.

## Settings

Configured the same way as `MeetingFlow.Monolith` itself: `appsettings.json` (committed,
empty placeholders) + `appsettings.Local.json` (gitignored, real values) + environment
variables (highest priority).

| Purpose | Config key | Environment variable |
|---|---|---|
| Evaluated model's API key | `AiChat:ApiKey` | `AiChat__ApiKey` |
| Evaluated model name | `AiChat:Model` | `AiChat__Model` |
| Evaluated model endpoint | `AiChat:Endpoint` | `AiChat__Endpoint` |
| Judge model's API key (defaults to `AiChat`'s) | `AiJudge:ApiKey` | `AiJudge__ApiKey` |
| Judge model name (defaults to `AiChat`'s) | `AiJudge:Model` | `AiJudge__Model` |
| Judge model endpoint (defaults to `AiChat`'s) | `AiJudge:Endpoint` | `AiJudge__Endpoint` |

The checked-in `appsettings.Local.json` uses Groq for both: `openai/gpt-oss-20b` as the
evaluated model, `openai/gpt-oss-120b` as the judge.

## Run it

```bash
dotnet run --project MeetingFlow.Monolith.Evals/MeetingFlow.Monolith.Evals.csproj
```

Run from anywhere; the project locates its own folder from the build output, so the
working directory doesn't matter. Cases are read from `cases/*.json` (one JSON file per
scenario: `caseId`, `dishes`, `notes`). One deep run makes two real model calls per case
(evaluated model + judge), sequentially to stay under free-tier rate limits.

## Report

Written to `MeetingFlow.Monolith.Evals/eval-report.md`, overwritten on every run. Contains
the run date, both model names, pass count, average judge score, a per-case results table,
and an auto-generated conclusion listing which cases failed and why.
