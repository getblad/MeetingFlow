using System.Net.Http.Json;
using DataAccessor.Contracts;
using System.Net;

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

    public async Task<AccessorResult<AttendeeDetailsDto>> CreateAttendeeAsync(
        DataAccessor.Contracts.CreateAttendeeRequest request)
    {
        using var response = await _http.PostAsJsonAsync("/data/attendees", request);
        return await AccessorResult<AttendeeDetailsDto>.FromResponseAsync(response);
    }

    public async Task<HttpStatusCode> DeleteAttendeeAsync(Guid id)
    {
        using var response = await _http.DeleteAsync($"/data/attendees/{id}");
        return response.StatusCode;
    }

    public async Task<FeedbackDto> CreateFeedbackAsync(PersistFeedbackRequest body)
    {
        var response = await _http.PostAsJsonAsync("/data/feedback", body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedbackDto>()
            ?? throw new InvalidOperationException("DataAccessor returned an empty body.");
    }
}

public sealed record AccessorResult<T>(HttpStatusCode StatusCode, T? Value)
{
    public static async Task<AccessorResult<T>> FromResponseAsync(HttpResponseMessage response)
    {
        var value = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>()
            : default;
        return new AccessorResult<T>(response.StatusCode, value);
    }
}
