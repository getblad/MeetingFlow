using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests;

// Asserts the gateway REJECTS attempts by the client to set server-controlled
// fields on POST /registrations. All three currently FAIL because the baseline
// accepts the full Registration entity and stores whatever the client supplied.
[Collection(nameof(GatewayCollection))]
public class RegistrationRequestTests
{
    static readonly Guid SeededMeeting = Guid.Parse("b2000000-0000-0000-0000-000000000001");
    // Seed has 15 attendees, ids ...000000000001 through ...000000000015. We pick
    // the last one so we don't clash with seeded registrations on Meeting1 (ids 1..6).
    static readonly Guid SeededAttendee = Guid.Parse("e5000000-0000-0000-0000-000000000015");

    readonly GatewayFixture _fx;
    public RegistrationRequestTests(GatewayFixture fx) => _fx = fx;

    [Fact]
    public async Task ClientCannotSetInternalPaymentReference()
    {
        Skip.If(_fx);
        var resp = await _fx.Http.PostAsJsonAsync("registrations", new
        {
            meetingId = SeededMeeting,
            attendeeId = SeededAttendee,
            ticketType = "VIP",
            internalPaymentReference = "HACKED-REF-123"
        });

        // Either the gateway should reject the unknown field (400), or it should
        // accept the request and silently drop the field. The forbidden outcome
        // is accepting it and persisting "HACKED-REF-123" anywhere reachable.
        if (resp.StatusCode == HttpStatusCode.BadRequest) return;

        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("HACKED-REF-123", body);
    }

    [Fact]
    public async Task ClientCannotSetPaymentStatus()
    {
        Skip.If(_fx);
        var resp = await _fx.Http.PostAsJsonAsync("registrations", new
        {
            meetingId = SeededMeeting,
            attendeeId = SeededAttendee,
            ticketType = "Student",
            paymentStatus = "Paid" // server, not client, decides if you've paid
        });

        if (resp.StatusCode == HttpStatusCode.BadRequest) return;
        Assert.True(resp.IsSuccessStatusCode, $"Unexpected status {(int)resp.StatusCode}");

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var registration = json.TryGetProperty("registration", out var r) ? r : json;
        Assert.True(registration.TryGetProperty("paymentStatus", out var ps), "No paymentStatus field returned");
        Assert.NotEqual("Paid", ps.GetString());
    }

    [Fact]
    public async Task ClientCannotSupplyOwnRegistrationId()
    {
        Skip.If(_fx);
        // Fresh GUID each run so reruns don't trip the DB's unique-id constraint;
        // a real DTO refactor should drop the client-supplied id outright.
        var clientChosenId = Guid.NewGuid();
        var resp = await _fx.Http.PostAsJsonAsync("registrations", new
        {
            id = clientChosenId,
            meetingId = SeededMeeting,
            attendeeId = SeededAttendee,
            ticketType = "General"
        });

        if (resp.StatusCode == HttpStatusCode.BadRequest) return;
        Assert.True(resp.IsSuccessStatusCode, $"Unexpected status {(int)resp.StatusCode}");

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var registration = json.TryGetProperty("registration", out var r) ? r : json;
        Assert.True(registration.TryGetProperty("id", out var idProp), "No id field returned");
        Assert.NotEqual(clientChosenId, idProp.GetGuid());
    }
}
