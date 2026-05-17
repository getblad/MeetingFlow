using System.Net.Http.Json;
using Gateway.Models;

namespace Gateway.Clients;

public class MeetingsManagerClient
{
    readonly HttpClient _http;
    public MeetingsManagerClient(HttpClient http) => _http = http;

    public async Task<List<Meeting>> GetAllAsync()
        => await _http.GetFromJsonAsync<List<Meeting>>("/meetings") ?? new();

    public async Task<HttpResponseMessage> GetByIdRawAsync(Guid id)
        => await _http.GetAsync($"/meetings/{id}");

    public async Task<List<Meeting>> GetAdminListAsync()
        => await _http.GetFromJsonAsync<List<Meeting>>("/admin/meetings") ?? new();

    public async Task<HttpResponseMessage> UpdateRawAsync(Guid id, HttpContent body)
        => await _http.PutAsync($"/meetings/{id}", body);

    public async Task<HttpResponseMessage> GetSpeakersRawAsync()
        => await _http.GetAsync("/speakers");

    public async Task<HttpResponseMessage> GetSpeakerByIdRawAsync(Guid id)
        => await _http.GetAsync($"/speakers/{id}");
}
