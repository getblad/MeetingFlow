using System.Net.Http.Json;
using MeetingsManager.Models;

namespace MeetingsManager.Clients;

public class SchedulingEngineClient
{
    readonly HttpClient _http;
    public SchedulingEngineClient(HttpClient http) => _http = http;

    // Intentionally sends the full Session entity (with InternalNotes, SpeakerId, Description...)
    // when only StartsAt/EndsAt/RoomName are needed.
    public async Task<bool> HasConflictAsync(Session candidate, IEnumerable<Session> existing)
    {
        var body = new { Candidate = candidate, Existing = existing.ToList() };
        var resp = await _http.PostAsJsonAsync("/scheduling/check-conflict", body);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<ConflictResult>();
        return json?.Conflict ?? false;
    }

    record ConflictResult(bool Conflict);
}
