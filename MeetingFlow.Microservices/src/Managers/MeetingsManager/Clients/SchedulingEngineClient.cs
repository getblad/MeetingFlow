using System.Net.Http.Json;
using SchedulingEngine.Contracts;

namespace MeetingsManager.Clients;

public class SchedulingEngineClient
{
    readonly HttpClient _http;
    public SchedulingEngineClient(HttpClient http) => _http = http;

    public async Task<CheckConflictResult> CheckConflictAsync(
        SessionSlotDto candidate,
        IReadOnlyList<SessionSlotDto> existing)
    {
        var response = await _http.PostAsJsonAsync(
            "/scheduling/check-conflict",
            new CheckConflictRequest(candidate, existing));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CheckConflictResult>()
            ?? throw new InvalidOperationException("SchedulingEngine returned an empty body.");
    }
}
