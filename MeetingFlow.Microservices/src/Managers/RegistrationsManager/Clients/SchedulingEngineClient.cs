using System.Net.Http.Json;
using SchedulingEngine.Contracts;

namespace RegistrationsManager.Clients;

public class SchedulingEngineClient
{
    readonly HttpClient _http;
    public SchedulingEngineClient(HttpClient http) => _http = http;

    public async Task<CheckCapacityResult> CheckCapacityAsync(
        int venueCapacity,
        int currentRegistrationCount)
    {
        var response = await _http.PostAsJsonAsync(
            "/scheduling/check-capacity",
            new CheckCapacityRequest(venueCapacity, currentRegistrationCount));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CheckCapacityResult>()
            ?? throw new InvalidOperationException("SchedulingEngine returned an empty body.");
    }
}
