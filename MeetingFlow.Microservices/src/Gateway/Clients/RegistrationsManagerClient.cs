namespace Gateway.Clients;

public class RegistrationsManagerClient
{
    readonly HttpClient _http;
    public RegistrationsManagerClient(HttpClient http) => _http = http;

    public async Task<HttpResponseMessage> CreateRegistrationRawAsync(HttpContent body)
        => await _http.PostAsync("/registrations", body);

    public async Task<HttpResponseMessage> GetRegistrationsByMeetingRawAsync(Guid meetingId)
        => await _http.GetAsync($"/registrations/by-meeting/{meetingId}");

    public async Task<HttpResponseMessage> CreateFeedbackRawAsync(HttpContent body)
        => await _http.PostAsync("/feedback", body);
}
