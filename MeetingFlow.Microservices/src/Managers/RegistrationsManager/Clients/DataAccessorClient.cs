using System.Net.Http.Json;
using DataAccessor.Contracts;

namespace RegistrationsManager.Clients;

public class DataAccessorClient
{
    readonly HttpClient _http;
    public DataAccessorClient(HttpClient http) => _http = http;

    public async Task<RegistrationMeetingContextDto?> GetRegistrationContextAsync(Guid id)
    {
        var response = await _http.GetAsync($"/data/meetings/{id}/registration-context");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationMeetingContextDto>();
    }

    public async Task<IReadOnlyList<RegistrationDto>> GetRegistrationsForMeetingAsync(Guid meetingId)
        => await _http.GetFromJsonAsync<List<RegistrationDto>>(
            $"/data/registrations/by-meeting/{meetingId}") ?? [];

    public async Task<RegistrationDto> CreateRegistrationAsync(PersistRegistrationRequest body)
    {
        var response = await _http.PostAsJsonAsync("/data/registrations", body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationDto>()
            ?? throw new InvalidOperationException("DataAccessor returned an empty body.");
    }

    public async Task<AttendeeContactDto?> GetAttendeeContactAsync(Guid id)
    {
        var response = await _http.GetAsync($"/data/attendees/{id}/contact");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttendeeContactDto>();
    }

    public async Task<FeedbackDto> CreateFeedbackAsync(PersistFeedbackRequest body)
    {
        var response = await _http.PostAsJsonAsync("/data/feedback", body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedbackDto>()
            ?? throw new InvalidOperationException("DataAccessor returned an empty body.");
    }
}
