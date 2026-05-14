using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Data;
using MeetingFlow.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MeetingFlowDbContext>(options =>
    options.UseSqlite("Data Source=meetingflow_api.db"));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// CORS — allow the React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// GraphQL — Hot Chocolate
// Educational baseline:
// GraphQL exposes EF Core entity types directly here.
// In production, consider separating GraphQL schema types from persistence models.
builder.Services
    .AddGraphQLServer()
    .AddQueryType<GraphQlSetup.Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

// Auto-create and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MeetingFlowDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

app.UseCors();

// Map REST endpoints
app.MapMeetingsEndpoints();
app.MapSpeakersEndpoints();
app.MapRegistrationsEndpoints();
app.MapDashboardEndpoints();
app.MapAuditLogEndpoints();

// Map GraphQL
app.MapGraphQL();

app.Run();
