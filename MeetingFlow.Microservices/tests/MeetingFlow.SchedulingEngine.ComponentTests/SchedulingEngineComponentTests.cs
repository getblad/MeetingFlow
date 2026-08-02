using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using SchedulingEngine.Contracts;
using Xunit;

namespace MeetingFlow.SchedulingEngine.ComponentTests;

public sealed class SchedulingEngineComponentTests(
    WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CheckConflict_WhenSessionsOverlapInSameRoom_ReturnsConflict()
    {
        // Arrange
        var existing = Session(
            roomName: "Main Hall",
            startsAt: "2026-09-10T10:00:00Z",
            endsAt: "2026-09-10T11:00:00Z");
        var candidate = Session(
            roomName: "main hall",
            startsAt: "2026-09-10T10:30:00Z",
            endsAt: "2026-09-10T11:30:00Z");

        // Act
        var response = await _client.PostAsJsonAsync(
            "/scheduling/check-conflict",
            new CheckConflictRequest(candidate, [existing]));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckConflictResult>();
        Assert.NotNull(result);
        Assert.True(result.HasConflict);
    }

    [Fact]
    public async Task CheckConflict_WhenSessionsAreAdjacent_ReturnsNoConflict()
    {
        // Arrange
        var existing = Session(
            roomName: "Main Hall",
            startsAt: "2026-09-10T10:00:00Z",
            endsAt: "2026-09-10T11:00:00Z");
        var candidate = Session(
            roomName: "Main Hall",
            startsAt: "2026-09-10T11:00:00Z",
            endsAt: "2026-09-10T12:00:00Z");

        // Act
        var response = await _client.PostAsJsonAsync(
            "/scheduling/check-conflict",
            new CheckConflictRequest(candidate, [existing]));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckConflictResult>();
        Assert.NotNull(result);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckConflict_WhenCandidateHasInvalidTimeRange_ReturnsValidationProblem()
    {
        // Arrange
        var candidate = Session(
            roomName: "Main Hall",
            startsAt: "2026-09-10T12:00:00Z",
            endsAt: "2026-09-10T11:00:00Z");

        // Act
        var response = await _client.PostAsJsonAsync(
            "/scheduling/check-conflict",
            new CheckConflictRequest(candidate, []));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("candidate", problem.Errors.Keys);
    }

    [Fact]
    public async Task CheckCapacity_WhenPlacesAreAvailable_ReturnsRemainingPlaces()
    {
        // Arrange
        var request = new CheckCapacityRequest(
            VenueCapacity: 100,
            CurrentRegistrationCount: 76);

        // Act
        var response = await _client.PostAsJsonAsync(
            "/scheduling/check-capacity",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckCapacityResult>();
        Assert.NotNull(result);
        Assert.True(result.HasCapacity);
        Assert.Equal(24, result.AvailablePlaces);
    }

    [Fact]
    public async Task CheckCapacity_WhenCapacityIsNegative_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CheckCapacityRequest(
            VenueCapacity: -1,
            CurrentRegistrationCount: 0);

        // Act
        var response = await _client.PostAsJsonAsync(
            "/scheduling/check-capacity",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("capacity", problem.Errors.Keys);
    }

    private static SessionSlotDto Session(
        string roomName,
        string startsAt,
        string endsAt) =>
        new(
            Guid.NewGuid(),
            roomName,
            DateTimeOffset.Parse(startsAt),
            DateTimeOffset.Parse(endsAt));

    public void Dispose() => _client.Dispose();
}
