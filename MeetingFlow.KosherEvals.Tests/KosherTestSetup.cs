using System.ClientModel;
using MeetingFlow.Monolith.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

namespace MeetingFlow.KosherEvals.Tests;

public sealed class KosherTestSetup : IDisposable
{
    private readonly IChatClient chatClient;
    private readonly IChatClient judgeClient;
    private readonly SemaphoreSlim requestGate;

    public OpenAiKosherAssessmentService Service { get; }
    public LlmJudge Judge { get; }
    public string Model { get; } = Environment.GetEnvironmentVariable("AiChat__Model") ?? "gpt-5-mini";
    public string JudgeModel { get; } = Environment.GetEnvironmentVariable("AiJudge__Model") ?? "openai/gpt-oss-120b";

    public KosherTestSetup()
    {
        chatClient = CreateChatClient("AiChat", Model, "https://api.openai.com/v1");
        try
        {
            judgeClient = CreateChatClient("AiJudge", JudgeModel, "https://api.groq.com/openai/v1");
        }
        catch
        {
            chatClient.Dispose();
            throw;
        }

        // The test needs only one concurrent request to the model.
        requestGate = new SemaphoreSlim(1, 1);
        Service = new OpenAiKosherAssessmentService(
            chatClient,
            NullLogger<OpenAiKosherAssessmentService>.Instance,
            requestGate);

        // The judge has its own model and API key, separate from the evaluated service.
        Judge = new LlmJudge(judgeClient);
    }

    private static IChatClient CreateChatClient(string prefix, string model, string defaultEndpoint)
    {
        // Read API keys only from the environment, never from source code.
        var endpoint = Environment.GetEnvironmentVariable($"{prefix}__Endpoint") ?? defaultEndpoint;
        var apiKey = Environment.GetEnvironmentVariable($"{prefix}__ApiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Set {prefix}__ApiKey before running the eval test.");
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return client.GetChatClient(model).AsIChatClient();
    }

    public void Dispose()
    {
        // The using statement in the test calls this method to release resources.
        try
        {
            chatClient.Dispose();
        }
        finally
        {
            try
            {
                judgeClient.Dispose();
            }
            finally
            {
                requestGate.Dispose();
            }
        }
    }
}
