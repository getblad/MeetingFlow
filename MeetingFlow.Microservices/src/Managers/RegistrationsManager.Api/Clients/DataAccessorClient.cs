using System.Net.Http.Json;
using RegistrationsManager.Models;

namespace RegistrationsManager.Clients;

public class DataAccessorClient
{
    readonly HttpClient _http;
    public DataAccessorClient(HttpClient http) => _http = http;

    // Fetches the WHOLE Meeting graph (with InternalNotes, AdminOnlyCode, Sessions...)
    // just to read venue capacity for the capacity check and inline pricing.
    public async Task<Meeting?> GetMeetingAsync(Guid id)
        => await _http.GetFromJsonAsync<Meeting>($"/data/meetings/{id}");

    public async Task<List<Registration>> GetRegistrationsForMeetingAsync(Guid meetingId)
        => await _http.GetFromJsonAsync<List<Registration>>($"/data/registrations/by-meeting/{meetingId}") ?? new();

    public async Task<Registration?> CreateRegistrationAsync(Registration body)
    {
        var resp = await _http.PostAsJsonAsync("/data/registrations", body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Registration>();
    }

    public async Task<Attendee?> GetAttendeeAsync(Guid id)
        => await _http.GetFromJsonAsync<Attendee>($"/data/attendees/{id}");

    public async Task<Feedback?> CreateFeedbackAsync(Feedback body)
    {
        var resp = await _http.PostAsJsonAsync("/data/feedback", body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Feedback>();
    }
}
