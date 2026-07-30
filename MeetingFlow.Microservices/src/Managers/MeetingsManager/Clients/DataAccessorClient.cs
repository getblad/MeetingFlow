using System.Net.Http.Json;
using DataAccessor.Contracts;

namespace MeetingsManager.Clients;

public class DataAccessorClient
{
    readonly HttpClient _http;
    public DataAccessorClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<MeetingSummaryDto>> GetAllMeetingsAsync()
        => await _http.GetFromJsonAsync<List<MeetingSummaryDto>>("/data/meetings") ?? [];

    public async Task<MeetingDetailsDto?> GetMeetingAsync(Guid id)
    {
        var response = await _http.GetAsync($"/data/meetings/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeetingDetailsDto>();
    }

    public async Task<MeetingDetailsDto?> UpdateMeetingAsync(
        Guid id,
        DataAccessor.Contracts.UpdateMeetingRequest body)
    {
        var response = await _http.PutAsJsonAsync($"/data/meetings/{id}", body);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeetingDetailsDto>();
    }

    public async Task<IReadOnlyList<SessionDto>> GetSessionsForMeetingAsync(Guid meetingId)
        => await _http.GetFromJsonAsync<List<SessionDto>>(
            $"/data/meetings/{meetingId}/sessions") ?? [];

    public async Task<IReadOnlyList<SpeakerDto>> GetSpeakersAsync()
        => await _http.GetFromJsonAsync<List<SpeakerDto>>("/data/speakers") ?? [];

    public async Task<SpeakerDto?> GetSpeakerAsync(Guid id)
    {
        var response = await _http.GetAsync($"/data/speakers/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SpeakerDto>();
    }

    public async Task<IReadOnlyList<AdminMeetingDto>> GetAdminMeetingsAsync()
        => await _http.GetFromJsonAsync<List<AdminMeetingDto>>("/data/admin/meetings") ?? [];
}
