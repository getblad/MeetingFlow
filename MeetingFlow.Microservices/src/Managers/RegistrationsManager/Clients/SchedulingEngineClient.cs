using System.Net.Http.Json;
using RegistrationsManager.Models;

namespace RegistrationsManager.Clients;

public class SchedulingEngineClient
{
    readonly HttpClient _http;
    public SchedulingEngineClient(HttpClient http) => _http = http;

    // Sends the entire Meeting entity to check capacity.
    public async Task<bool> HasCapacityAsync(Meeting meeting, int venueCapacity, int currentRegistrationCount)
    {
        var body = new { Meeting = meeting, VenueCapacity = venueCapacity, CurrentRegistrationCount = currentRegistrationCount };
        var resp = await _http.PostAsJsonAsync("/scheduling/check-capacity", body);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<CapacityResult>();
        return result?.HasCapacity ?? false;
    }

    record CapacityResult(bool HasCapacity, int Available, string MeetingTitle);
}
