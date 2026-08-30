using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using MeetingFlow.Monolith.Data;
using MeetingFlow.Monolith.Services;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<MeetingFlowDbContext>(options =>
    options.UseSqlite("Data Source=meetingflow_monolith.db"));
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
        KosherCheckTrafficControl.CreatePartition);
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many kosher checks. Please wait a minute and try again." },
            cancellationToken);
    };
});

var aiModel = builder.Configuration["AiChat:Model"] ?? "gpt-5-mini";
var aiEndpoint = builder.Configuration["AiChat:Endpoint"] ?? "https://api.openai.com/v1";
var aiApiKey = builder.Configuration["AiChat:ApiKey"];

if (!string.IsNullOrWhiteSpace(aiApiKey))
{
    var openAiOptions = new OpenAIClientOptions { Endpoint = new Uri(aiEndpoint) };
    var openAiClient = new OpenAIClient(
        new System.ClientModel.ApiKeyCredential(aiApiKey),
        openAiOptions);
    builder.Services.AddSingleton<IChatClient>(openAiClient.GetChatClient(aiModel).AsIChatClient());
    builder.Services.AddSingleton(new SemaphoreSlim(4, 4));
    builder.Services.AddSingleton<IKosherAssessmentService, OpenAiKosherAssessmentService>();
}
else
{
    builder.Services.AddSingleton<IKosherAssessmentService, UnavailableKosherAssessmentService>();
}

var app = builder.Build();

// Auto-create and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MeetingFlowDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseWhen(
    KosherCheckTrafficControl.AppliesTo,
    branch => branch.UseRateLimiter());
app.MapRazorPages();

app.Run();
