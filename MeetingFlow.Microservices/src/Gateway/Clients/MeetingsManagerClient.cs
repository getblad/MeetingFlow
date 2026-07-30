using System.Net.Http.Json;
using MeetingsManager.Contracts;

namespace Gateway.Clients;

public class MeetingsManagerClient
{
    readonly HttpClient _http;
    public MeetingsManagerClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<MeetingListItemDto>> GetAllAsync()
        => await _http.GetFromJsonAsync<List<MeetingListItemDto>>("/meetings") ?? [];

    public async Task<MeetingDetailsDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/meetings/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeetingDetailsDto>();
    }

    public async Task<DownstreamResult<MeetingDetailsDto>> UpdateAsync(
        Guid id,
        MeetingsManager.Contracts.UpdateMeetingRequest request)
    {
        var response = await _http.PutAsJsonAsync($"/meetings/{id}", request);
        return await DownstreamResult<MeetingDetailsDto>.FromResponseAsync(response);
    }

    public async Task<IReadOnlyList<SpeakerDto>> GetSpeakersAsync()
        => await _http.GetFromJsonAsync<List<SpeakerDto>>("/speakers") ?? [];

    public async Task<SpeakerDto?> GetSpeakerByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/speakers/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SpeakerDto>();
    }
}
