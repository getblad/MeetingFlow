using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingFlow.Monolith.Evals;
using MeetingFlow.Monolith.Models;
using MeetingFlow.Monolith.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

var projectDirectory = FindProjectDirectory();

var configuration = new ConfigurationBuilder()
    .SetBasePath(projectDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

string RequireSetting(string key) =>
    configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException(
            $"'{key}' is not configured. Set it in appsettings.Local.json or as the environment variable " +
            $"'{key.Replace(":", "__")}'.");

var evaluatedModel = RequireSetting("AiChat:Model");
var evaluatedEndpoint = configuration["AiChat:Endpoint"] ?? "https://api.openai.com/v1";
var evaluatedApiKey = RequireSetting("AiChat:ApiKey");

var judgeModel = configuration["AiJudge:Model"] is { Length: > 0 } configuredJudgeModel
    ? configuredJudgeModel
    : evaluatedModel;
var judgeEndpoint = configuration["AiJudge:Endpoint"] is { Length: > 0 } configuredJudgeEndpoint
    ? configuredJudgeEndpoint
    : evaluatedEndpoint;
var judgeApiKey = configuration["AiJudge:ApiKey"] is { Length: > 0 } configuredJudgeApiKey
    ? configuredJudgeApiKey
    : evaluatedApiKey;

var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false) }
};

var evaluatedClient = BuildChatClient(evaluatedEndpoint, evaluatedApiKey, evaluatedModel);
var judgeClient = BuildChatClient(judgeEndpoint, judgeApiKey, judgeModel);

var assessmentService = new OpenAiKosherAssessmentService(
    evaluatedClient,
    NullLogger<OpenAiKosherAssessmentService>.Instance,
    new SemaphoreSlim(1, 1));

var casesDirectory = Path.Combine(projectDirectory, "cases");
if (!Directory.Exists(casesDirectory))
{
    throw new DirectoryNotFoundException($"No 'cases' directory found at {casesDirectory}.");
}

var caseFiles = Directory.GetFiles(casesDirectory, "*.json").OrderBy(path => path, StringComparer.Ordinal);
var results = new List<CaseResult>();

foreach (var caseFile in caseFiles)
{
    var evalCase = JsonSerializer.Deserialize<EvalCase>(
        File.ReadAllText(caseFile),
        new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException($"Could not read case file: {caseFile}");

    Console.WriteLine($"Running case '{evalCase.CaseId}'...");

    var entries = evalCase.Dishes
        .Select((dish, index) => new DishCheckEntry($"dish-{index + 1}", dish))
        .ToList();

    try
    {
        var response = await assessmentService.AssessAsync(entries);
        var deterministic = DeterministicChecks.Run(evalCase, response);
        var judgeVerdict = await Judge.GradeAsync(judgeClient, evalCase, response, serializerOptions, CancellationToken.None);
        results.Add(new CaseResult(evalCase, deterministic, judgeVerdict, RunError: null));
    }
    catch (Exception exception)
    {
        var detail = exception.InnerException?.Message ?? exception.Message;
        Console.WriteLine($"  Case '{evalCase.CaseId}' failed: {exception.Message} ({detail})");
        var failedCheck = new DeterministicCheckResult(
            Passed: false,
            FailureReasons: [$"The system did not return a usable response: {detail}"]);
        results.Add(new CaseResult(evalCase, failedCheck, Judge: null, RunError: exception.Message));
    }
}

var reportPath = Path.Combine(projectDirectory, "eval-report.md");
ReportWriter.Write(reportPath, evaluatedModel, judgeModel, results);

Console.WriteLine();
Console.WriteLine($"Ran {results.Count} case(s). Report written to {reportPath}");

static IChatClient BuildChatClient(string endpoint, string apiKey, string model)
{
    var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
    var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
    return client.GetChatClient(model).AsIChatClient();
}

static string FindProjectDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MeetingFlow.Monolith.Evals.csproj")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName
        ?? throw new InvalidOperationException(
            "Could not locate the MeetingFlow.Monolith.Evals project directory from the build output.");
}
