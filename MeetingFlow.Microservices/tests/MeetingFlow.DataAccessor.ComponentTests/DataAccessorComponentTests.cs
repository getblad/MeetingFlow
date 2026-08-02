using System.Net;
using System.Net.Http.Json;
using DataAccessor.Contracts;
using Xunit;

namespace MeetingFlow.DataAccessor.ComponentTests;

public sealed class DataAccessorComponentTests(DataAccessorFixture fixture)
    : IClassFixture<DataAccessorFixture>
{
    private static readonly Guid FrontendSummitId =
        Guid.Parse("b2000000-0000-0000-0000-000000000001");

    private static readonly Guid WorkshopWithoutRegistrationsId =
        Guid.Parse("b2000000-0000-0000-0000-000000000003");

    private static readonly Guid AttendeeId =
        Guid.Parse("e5000000-0000-0000-0000-000000000015");

    [Fact]
    public async Task GetMeeting_WhenMeetingExists_ReturnsGraphLoadedFromPostgreSql()
    {
        // Act
        var response = await fixture.Client.GetAsync($"/data/meetings/{FrontendSummitId}");

        // Assert: HTTP contract and the EF query with related entities both work.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var meeting = await response.Content.ReadFromJsonAsync<MeetingDetailsDto>();

        Assert.NotNull(meeting);
        Assert.Equal("Frontend Architecture Summit", meeting.Title);
        Assert.Equal("TechHub Convention Center", meeting.Venue?.Name);
        Assert.Equal(3, meeting.Sessions.Count);
        Assert.Equal(6, meeting.Registrations.Count);
        Assert.Equal(2, meeting.Feedback.Count);
        Assert.DoesNotContain("internalNotes", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adminOnlyCode", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRegistration_WhenReferencesExist_PersistsItInPostgreSql()
    {
        // Arrange: meeting 3 has no registrations in the seed data.
        var request = new PersistRegistrationRequest(
            WorkshopWithoutRegistrationsId,
            AttendeeId,
            "General");

        // Act: write through the HTTP API.
        var createResponse = await fixture.Client.PostAsJsonAsync(
            "/data/registrations",
            request);

        // Assert the server-owned fields returned by the write endpoint.
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Pending", created.PaymentStatus);

        // Read through a separate endpoint to prove the row was committed to
        // PostgreSQL rather than only returned from memory.
        var saved = await fixture.Client.GetFromJsonAsync<List<RegistrationDto>>(
            $"/data/registrations/by-meeting/{WorkshopWithoutRegistrationsId}");

        var registration = Assert.Single(saved!);
        Assert.Equal(created.Id, registration.Id);
        Assert.Equal(AttendeeId, registration.AttendeeId);
        Assert.Equal("General", registration.TicketType);
        Assert.Equal("Fatima Al-Rashid", registration.Attendee?.FullName);
    }

    [Fact]
    public async Task GetMeeting_WhenMeetingDoesNotExist_ReturnsNotFound()
    {
        // Act
        var response = await fixture.Client.GetAsync($"/data/meetings/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
