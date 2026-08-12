using System.Net.Http.Json;
using RegistrationsManager.Contracts;

namespace Gateway.Clients;

public class RegistrationsManagerClient
{
    readonly HttpClient _http;
    public RegistrationsManagerClient(HttpClient http) => _http = http;

    public async Task<DownstreamResult<CreateRegistrationResult>> CreateRegistrationAsync(
        CreateRegistrationRequest request)
    {
        var response = await _http.PostAsJsonAsync("/registrations", request);
        return await DownstreamResult<CreateRegistrationResult>.FromResponseAsync(response);
    }

    public async Task<DownstreamResult<AttendeeDto>> CreateAttendeeAsync(
        RegistrationsManager.Contracts.CreateAttendeeRequest request)
    {
        using var response = await _http.PostAsJsonAsync("/attendees", request);
        return await DownstreamResult<AttendeeDto>.FromResponseAsync(response);
    }

    public async Task<DownstreamStatus> DeleteAttendeeAsync(Guid id)
    {
        using var response = await _http.DeleteAsync($"/attendees/{id}");
        return await DownstreamStatus.FromResponseAsync(response);
    }

    public async Task<IReadOnlyList<RegistrationDto>> GetRegistrationsByMeetingAsync(
        Guid meetingId)
        => await _http.GetFromJsonAsync<List<RegistrationDto>>(
            $"/registrations/by-meeting/{meetingId}") ?? [];

    public async Task<DownstreamResult<FeedbackDto>> CreateFeedbackAsync(
        SubmitFeedbackRequest request)
    {
        var response = await _http.PostAsJsonAsync("/feedback", request);
        return await DownstreamResult<FeedbackDto>.FromResponseAsync(response);
    }
}
