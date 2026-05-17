using System.Net.Http.Json;
using MeetingsManager.Models;

namespace MeetingsManager.Clients;

// Typed HttpClient. Returns this service's local Meeting type — drift between this
// type and the DataAccessor's Meeting type will silently null out fields.
public class DataAccessorClient
{
    readonly HttpClient _http;
    public DataAccessorClient(HttpClient http) => _http = http;

    public async Task<List<Meeting>> GetAllMeetingsAsync()
        => await _http.GetFromJsonAsync<List<Meeting>>("/data/meetings") ?? new();

    public async Task<Meeting?> GetMeetingAsync(Guid id)
        => await _http.GetFromJsonAsync<Meeting>($"/data/meetings/{id}");

    public async Task<Meeting?> UpdateMeetingAsync(Guid id, Meeting body)
    {
        var resp = await _http.PutAsJsonAsync($"/data/meetings/{id}", body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Meeting>();
    }

    public async Task<List<Session>> GetSessionsForMeetingAsync(Guid meetingId)
        => await _http.GetFromJsonAsync<List<Session>>($"/data/meetings/{meetingId}/sessions") ?? new();

    public async Task<List<Speaker>> GetSpeakersAsync()
        => await _http.GetFromJsonAsync<List<Speaker>>("/data/speakers") ?? new();

    public async Task<Speaker?> GetSpeakerAsync(Guid id)
        => await _http.GetFromJsonAsync<Speaker>($"/data/speakers/{id}");
}
