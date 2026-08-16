namespace MeetingFlow.Api.Tests.UnitTestingLecture.Before;

// ❌ BEFORE: Hard-to-test service.
//
// NOTE on how this relates to the shipped code: RegistrationsEndpoints.cs receives its
// MeetingFlowDbContext by injection (not `new`) and today performs NO status, capacity or
// existence checks at all — it only shares problem 3 below, the ambient DateTimeOffset.UtcNow.
// This class is what that handler turns into once the rules are added inline, the quick way.
//
// Problems:
//   1. Creates its own data access — can't substitute in tests
//   2. Creates its own HttpClient — can't intercept notifications
//   3. Uses DateTimeOffset.UtcNow — non-deterministic, can't control time in tests
//   4. Uses exceptions for expected business outcomes (meeting not found, full, etc.)

public class HardToTestRegistrationService
{
    // ❌ No constructor dependencies — everything is created inside the method

    public async Task<string> RegisterAsync(
        Guid meetingId, string attendeeName, string attendeeEmail, string ticketType)
    {
        // ❌ Hidden dependency: creates its own database access
        var db = new MeetingFlowDb("Server=prod-db;Database=MeetingFlow;");

        var meeting = await db.GetMeetingAsync(meetingId)
            ?? throw new InvalidOperationException("Meeting not found");

        // ❌ Business logic uses exceptions for expected outcomes
        if (meeting.Status != "Published")
            throw new InvalidOperationException("Meeting is not open for registration");
        if (meeting.RegistrationCount >= meeting.VenueCapacity)
            throw new InvalidOperationException("Meeting is full");

        // ❌ Non-deterministic: uses ambient static time (same as RegistrationsEndpoints.cs)
        var registeredAt = DateTimeOffset.UtcNow;

        await db.SaveRegistrationAsync(meetingId, attendeeName, attendeeEmail, ticketType, registeredAt);

        // ❌ Hidden dependency: creates its own HTTP client for notifications
        using var http = new HttpClient();
        await http.PostAsync(
            "https://notifications.internal/api/registration-confirmation",
            new StringContent($"{{\"meetingId\":\"{meetingId}\",\"email\":\"{attendeeEmail}\"}}"));

        return "Registered";
    }
}

// Stubs to make the "before" example compile — not real implementations
internal class MeetingFlowDb(string connectionString)
{
    internal Task<MeetingRecord?> GetMeetingAsync(Guid id) =>
        throw new NotImplementedException("Teaching stub — " + connectionString);

    internal Task SaveRegistrationAsync(
        Guid meetingId, string name, string email, string ticketType, DateTimeOffset registeredAt) =>
        throw new NotImplementedException("Teaching stub");
}

internal record MeetingRecord(string Status, int RegistrationCount, int VenueCapacity);
