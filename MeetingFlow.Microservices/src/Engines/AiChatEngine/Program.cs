using AiChatEngine.Clients;
using AiChatEngine.Contracts;
using AiChatEngine.Services;
using DataAccessor.Contracts;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// In Docker, env vars like ServiceUrls__DataAccessor override appsettings.json automatically.
var dataAccessorUrl = builder.Configuration["ServiceUrls:DataAccessor"] ?? "http://localhost:5010";

// AI configuration — supports GitHub Models (Copilot), OpenAI, or Azure OpenAI.
var aiModel = builder.Configuration["AiChat:Model"] ?? "gpt-4o-mini";
var aiEndpoint = builder.Configuration["AiChat:Endpoint"] ?? "https://models.inference.ai.azure.com";
var aiApiKey = builder.Configuration["AiChat:ApiKey"] ?? "";

builder.Services.AddHttpClient<DataAccessorClient>(c => c.BaseAddress = new Uri(dataAccessorUrl));

// Use AI provider if key is available, otherwise fall back to rule-based (great for testing).
if (!string.IsNullOrEmpty(aiApiKey))
{
    var options = new OpenAIClientOptions { Endpoint = new Uri(aiEndpoint) };
    var client = new OpenAIClient(new System.ClientModel.ApiKeyCredential(aiApiKey), options);
    builder.Services.AddSingleton<IChatClient>(client.GetChatClient(aiModel).AsIChatClient());
    builder.Services.AddSingleton<IChatService, OpenAiChatService>();
}
else
{
    builder.Services.AddSingleton<IChatService, RuleBasedChatService>();
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "AiChatEngine" }));

app.MapPost("/chat", async (ChatRequest request, IChatService chat, DataAccessorClient data) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["message"] = ["Message is required."]
        });
    }

    var history = request.History?
        .Select(message => new AiChatEngine.Services.ChatMessage(
            message.Role,
            message.Content))
        .ToList() ?? [];
    var response = await chat.ProcessAsync(request.Message.Trim(), history);

    object? actionResult = null;
    if (response.Action is not null)
    {
        actionResult = await ExecuteActionAsync(response.Action, data);
    }

    var action = response.Action is null
        ? null
        : new ChatActionDto(response.Action.Type, response.Action.Parameters);

    return Results.Ok(new ChatResult(response.Reply, action, actionResult));
});

app.Run();

static async Task<object?> ExecuteActionAsync(ChatAction action, DataAccessorClient data)
{
    return action.Type switch
    {
        "list_meetings" => await data.GetMeetingsAsync(),
        "get_meeting" => action.Parameters.TryGetValue("meetingId", out var mid) && Guid.TryParse(mid, out var meetingId)
            ? await data.GetMeetingAsync(meetingId) : null,
        "list_tasks" => action.Parameters.TryGetValue("meetingId", out var tid) && Guid.TryParse(tid, out var tmId)
            ? await data.GetTasksByMeetingAsync(tmId) : await data.GetTasksAsync(),
        "create_task" => await CreateTaskAsync(action, data),
        "complete_task" => action.Parameters.TryGetValue("taskId", out var ctid) && Guid.TryParse(ctid, out var completeId)
            ? await data.CompleteTaskAsync(completeId) : null,
        "delete_task" => action.Parameters.TryGetValue("taskId", out var dtid) && Guid.TryParse(dtid, out var deleteId)
            ? await data.DeleteTaskAsync(deleteId) : null,
        _ => null
    };
}

static async Task<object?> CreateTaskAsync(ChatAction action, DataAccessorClient data)
{
    action.Parameters.TryGetValue("title", out var title);
    action.Parameters.TryGetValue("meetingId", out var meetingIdStr);
    action.Parameters.TryGetValue("assignedTo", out var assignedTo);

    if (string.IsNullOrEmpty(title)) return null;

    // If no meetingId specified, use the first available meeting.
    Guid meetingId;
    if (!Guid.TryParse(meetingIdStr, out meetingId))
    {
        var meetings = await data.GetMeetingsAsync();
        if (meetings.Count == 0) return null;
        meetingId = meetings[0].Id;
    }

    var task = new CreateMeetingTaskRequest(meetingId, title, assignedTo);

    return await data.CreateTaskAsync(task);
}
