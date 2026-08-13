using System.Net;
using System.Net.Http.Json;
using DataAccessor.Contracts;
using DataAccessor.Models;
using Xunit;

namespace MeetingFlow.DataAccessor.ComponentTests;

public sealed class DataAccessorComponentTests(DataAccessorFixture fixture)
    : IClassFixture<DataAccessorFixture>, IAsyncLifetime
{
    // xUnit creates a new test-class instance for every Fact. The PostgreSQL
    // container stays alive for the class, while Respawn gives every Fact an
    // empty set of application tables.
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMeeting_WhenMeetingExists_ReturnsGraphLoadedFromPostgreSql()
    {
        // Arrange: Respawn has removed production seed data, so this test owns
        // the complete graph it is going to assert on.
        var meetingId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var speakerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var attendee = new Attendee
        {
            Id = attendeeId,
            FullName = "Component Test Attendee",
            Email = $"attendee-{attendeeId:N}@component.test"
        };

        var meetingEntity = new Meeting
        {
            Id = meetingId,
            Title = "Component Test Meeting",
            Description = "A meeting created only for this component test.",
            Status = "Published",
            StartsAt = now.AddDays(10),
            EndsAt = now.AddDays(10).AddHours(2),
            CreatedAt = now,
            InternalNotes = "Must not cross the HTTP boundary.",
            AdminOnlyCode = "COMPONENT-SECRET",
            VenueId = venueId,
            Venue = new Venue
            {
                Id = venueId,
                Name = "Component Test Venue",
                Address = "1 Test Street",
                City = "Test City",
                Capacity = 20
            },
            Sessions =
            [
                new Session
                {
                    Id = Guid.NewGuid(),
                    Title = "Component Test Session",
                    Description = "A session created by the test.",
                    StartsAt = now.AddDays(10),
                    EndsAt = now.AddDays(10).AddHours(1),
                    RoomName = "Test Room",
                    SpeakerId = speakerId,
                    Speaker = new Speaker
                    {
                        Id = speakerId,
                        FullName = "Component Test Speaker",
                        Bio = "Test speaker bio",
                        Email = "speaker@component.test"
                    }
                }
            ],
            Registrations =
            [
                new Registration
                {
                    Id = Guid.NewGuid(),
                    AttendeeId = attendeeId,
                    Attendee = attendee,
                    RegisteredAt = now,
                    TicketType = "General",
                    PaymentStatus = "Pending"
                }
            ],
            Feedback =
            [
                new Feedback
                {
                    Id = Guid.NewGuid(),
                    AttendeeId = attendeeId,
                    Attendee = attendee,
                    Rating = 5,
                    Comment = "Created by the component test.",
                    CreatedAt = now
                }
            ]
        };

        await fixture.SeedAsync(meetingEntity);

        // Act
        var response = await fixture.Client.GetAsync($"/data/meetings/{meetingId}");

        // Assert: HTTP contract and the EF query with related entities both work.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var meeting = await response.Content.ReadFromJsonAsync<MeetingDetailsDto>();

        Assert.NotNull(meeting);
        Assert.Equal("Component Test Meeting", meeting.Title);
        Assert.Equal("Component Test Venue", meeting.Venue?.Name);
        Assert.Single(meeting.Sessions);
        Assert.Single(meeting.Registrations);
        Assert.Single(meeting.Feedback);
        Assert.DoesNotContain("internalNotes", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adminOnlyCode", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRegistration_WhenReferencesExist_PersistsItInPostgreSql()
    {
        // Arrange: create only the references required by this scenario.
        var meetingId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await fixture.SeedAsync(
            new Venue
            {
                Id = venueId,
                Name = "Registration Test Venue",
                Address = "2 Test Street",
                City = "Test City",
                Capacity = 10
            });

        await fixture.SeedAsync(
            new Meeting
            {
                Id = meetingId,
                Title = "Registration Test Meeting",
                Description = "A meeting created only for the registration test.",
                Status = "Published",
                StartsAt = now.AddDays(20),
                EndsAt = now.AddDays(20).AddHours(2),
                CreatedAt = now,
                VenueId = venueId
            });

        await fixture.SeedAsync(
            new Attendee
            {
                Id = attendeeId,
                FullName = "Registration Test Attendee",
                Email = $"attendee-{attendeeId:N}@component.test"
            });

        var request = new PersistRegistrationRequest(
            meetingId,
            attendeeId,
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
            $"/data/registrations/by-meeting/{meetingId}");

        var registration = Assert.Single(saved!);
        Assert.Equal(created.Id, registration.Id);
        Assert.Equal(attendeeId, registration.AttendeeId);
        Assert.Equal("General", registration.TicketType);
        Assert.Equal("Registration Test Attendee", registration.Attendee?.FullName);
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
