using System.Text.Json;
using Xunit;

namespace MeetingFlow.Microservices.IntegrationTests;

// Asserts the PUBLIC gateway never serves internal-only fields.
// All of these are expected to FAIL against the current baseline — that is the
// teaching signal. After students refactor in proper edge DTOs, they should pass.
[Collection(nameof(GatewayCollection))]
public class MeetingLeakageTests
{
    static readonly string[] ForbiddenFields =
    [
        "internalNotes",
        "adminOnlyCode",
        "internalPaymentReference",
        "internalContactName",
        "internalContactPhone",
        "rawPayloadJson",
        "moderationNotes",
        "technicalDetails"
    ];

    readonly GatewayFixture _fx;
    public MeetingLeakageTests(GatewayFixture fx) => _fx = fx;

    [Fact]
    public async Task PublicMeetingList_DoesNotLeakInternalFields()
    {
        Skip.If(_fx);
        var json = await _fx.Http.GetStringAsync("meetings");
        var leaks = FindForbiddenFields(JsonDocument.Parse(json).RootElement);
        Assert.Empty(leaks);
    }

    [Fact]
    public async Task PublicMeetingDetails_DoesNotLeakInternalFields()
    {
        Skip.If(_fx);
        var json = await _fx.Http.GetStringAsync("meetings/b2000000-0000-0000-0000-000000000001");
        var leaks = FindForbiddenFields(JsonDocument.Parse(json).RootElement);
        Assert.Empty(leaks);
    }

    [Fact]
    public async Task PublicSpeakerProfile_DoesNotLeakEmailPhoneOrInternalNotes()
    {
        Skip.If(_fx);
        var json = await _fx.Http.GetStringAsync("speakers/c3000000-0000-0000-0000-000000000001");
        var root = JsonDocument.Parse(json).RootElement;
        var leaked = new List<string>();
        foreach (var field in new[] { "email", "phone", "internalNotes" })
        {
            if (root.TryGetProperty(field, out var v) && v.ValueKind != JsonValueKind.Null)
                leaked.Add(field);
        }
        Assert.Empty(leaked);
    }

    [Fact]
    public async Task AdminEndpoint_RequiresAuth_OrIsNotPubliclyReachable()
    {
        Skip.If(_fx);
        var resp = await _fx.Http.GetAsync("admin/meetings");
        // Public, unauthenticated callers must not be able to enumerate admin data.
        // 401/403 is fine; 404 (route removed from public gateway) is also fine.
        // 200 with admin payload = smell.
        Assert.True(
            resp.StatusCode is System.Net.HttpStatusCode.Unauthorized
                            or System.Net.HttpStatusCode.Forbidden
                            or System.Net.HttpStatusCode.NotFound,
            $"Expected 401/403/404, got {(int)resp.StatusCode} {resp.StatusCode}");
    }

    // Recursively walks the JSON tree and returns the JSON-paths of any property
    // whose name matches the forbidden list and whose value is non-null.
    static List<string> FindForbiddenFields(JsonElement element, string path = "$")
    {
        var hits = new List<string>();
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var childPath = $"{path}.{prop.Name}";
                    if (ForbiddenFields.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)
                        && prop.Value.ValueKind != JsonValueKind.Null)
                    {
                        hits.Add($"{childPath} = {prop.Value}");
                    }
                    hits.AddRange(FindForbiddenFields(prop.Value, childPath));
                }
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                    hits.AddRange(FindForbiddenFields(item, $"{path}[{i++}]"));
                break;
        }
        return hits;
    }
}
