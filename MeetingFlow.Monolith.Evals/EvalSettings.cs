using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace MeetingFlow.Monolith.Evals;

/// <summary>Everything the run needs, resolved from config files, environment variables and command line flags.</summary>
public sealed class EvalSettings
{
    public required string ApiKey { get; init; }
    public required string Model { get; init; }
    public required string Endpoint { get; init; }

    public required string JudgeApiKey { get; init; }
    public required string JudgeModel { get; init; }
    public required string JudgeEndpoint { get; init; }

    public required string CasesFile { get; init; }
    public required string ReportDirectory { get; init; }
    public required string RepositoryRoot { get; init; }

    /// <summary>
    /// The case file written as a path relative to the repository root, so a report can be handed to
    /// someone else without exposing the local user's folder layout. A case file kept outside the
    /// repository is reduced to its file name for the same reason.
    /// </summary>
    public string CasesFileForDisplay
    {
        get
        {
            var relative = Path.GetRelativePath(RepositoryRoot, CasesFile);
            return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
                ? Path.GetFileName(CasesFile)
                : relative.Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    public required int Repeat { get; init; }
    public required int Concurrency { get; init; }
    public required int DelayMs { get; init; }
    public required int MaxRetries { get; init; }
    public required int TimeoutSeconds { get; init; }

    public required string[] Tags { get; init; }
    public required string[] CaseIds { get; init; }

    /// <summary>True when the judge and the evaluated system are the same model, which invites self-preference bias.</summary>
    public bool JudgeIsSameModel =>
        string.Equals(Model, JudgeModel, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Endpoint, JudgeEndpoint, StringComparison.OrdinalIgnoreCase);
}

public static class EvalConfiguration
{
    private static readonly Dictionary<string, string> SwitchMappings = new(StringComparer.Ordinal)
    {
        ["--model"] = "AiChat:Model",
        ["--endpoint"] = "AiChat:Endpoint",
        ["--api-key"] = "AiChat:ApiKey",
        ["--judge-model"] = "Eval:JudgeModel",
        ["--judge-endpoint"] = "Eval:JudgeEndpoint",
        ["--judge-api-key"] = "Eval:JudgeApiKey",
        ["--cases"] = "Eval:CasesFile",
        ["--report-dir"] = "Eval:ReportDirectory",
        ["--repeat"] = "Eval:Repeat",
        ["--concurrency"] = "Eval:Concurrency",
        ["--delay-ms"] = "Eval:DelayMs",
        ["--max-retries"] = "Eval:MaxRetries",
        ["--timeout-seconds"] = "Eval:TimeoutSeconds",
        ["--tag"] = "Eval:Tags",
        ["--case"] = "Eval:CaseIds"
    };

    /// <summary>
    /// Reads the monolith's own settings first, so an existing appsettings.Local.json with a working
    /// AiChat:ApiKey is enough to run the evals without configuring a second key.
    /// </summary>
    public static EvalSettings Build(string[] args)
    {
        var repositoryRoot = FindRepositoryRoot();
        var monolithDirectory = Path.Combine(repositoryRoot, "MeetingFlow.Monolith");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(monolithDirectory, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(monolithDirectory, "appsettings.Local.json"), optional: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Evals.json"), optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args, SwitchMappings)
            .Build();

        var apiKey = configuration["AiChat:ApiKey"] ?? string.Empty;
        var model = Fallback(configuration["AiChat:Model"], "gpt-5-mini");
        var endpoint = Fallback(configuration["AiChat:Endpoint"], "https://api.openai.com/v1");

        return new EvalSettings
        {
            ApiKey = apiKey,
            Model = model,
            Endpoint = endpoint,
            JudgeApiKey = Fallback(configuration["Eval:JudgeApiKey"], apiKey),
            JudgeModel = Fallback(configuration["Eval:JudgeModel"], model),
            JudgeEndpoint = Fallback(configuration["Eval:JudgeEndpoint"], endpoint),
            CasesFile = ResolvePath(
                configuration["Eval:CasesFile"],
                DefaultCasesFile(repositoryRoot)),
            ReportDirectory = ResolvePath(
                configuration["Eval:ReportDirectory"],
                Path.Combine(repositoryRoot, "MeetingFlow.Monolith.Evals", "reports")),
            Repeat = Number(configuration["Eval:Repeat"], 1, minimum: 1),
            Concurrency = Number(configuration["Eval:Concurrency"], 1, minimum: 1),
            DelayMs = Number(configuration["Eval:DelayMs"], 500, minimum: 0),
            MaxRetries = Number(configuration["Eval:MaxRetries"], 3, minimum: 0),
            TimeoutSeconds = Number(configuration["Eval:TimeoutSeconds"], 90, minimum: 5),
            RepositoryRoot = repositoryRoot,
            Tags = SplitList(configuration["Eval:Tags"]),
            CaseIds = SplitList(configuration["Eval:CaseIds"])
        };
    }

    /// <summary>
    /// Prefers the case file in the project folder over the copy in the build output, so editing a
    /// case or adding a new one takes effect on the next run without rebuilding.
    /// </summary>
    private static string DefaultCasesFile(string repositoryRoot)
    {
        var source = Path.Combine(repositoryRoot, "MeetingFlow.Monolith.Evals", "Cases", "kosher-cases.json");
        return File.Exists(source)
            ? source
            : Path.Combine(AppContext.BaseDirectory, "Cases", "kosher-cases.json");
    }

    private static string Fallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string ResolvePath(string? value, string fallback) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(value) ? fallback : value.Trim());

    private static int Number(string? value, int fallback, int minimum) =>
        int.TryParse(value, out var parsed) && parsed >= minimum ? parsed : fallback;

    private static string[] SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Walks up from the build output until the solution file appears, so the run works from any directory.</summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MeetingFlow.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
