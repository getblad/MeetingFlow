using System.Net.Http.Json;
using RegistrationsManager.Models;

namespace RegistrationsManager.Clients;

public class NotificationsAccessorClient
{
    readonly HttpClient _http;
    public NotificationsAccessorClient(HttpClient http) => _http = http;

    // Sends the FULL Attendee + FULL Meeting just to send a confirmation email.
    // The accessor only needs (toEmail, subject, body).
    public async Task SendRegistrationConfirmationAsync(Attendee attendee, Meeting meeting)
    {
        var body = new
        {
            Attendee = attendee,
            Meeting = meeting,
            Channel = "Email",
            Body = $"Hi {attendee.FullName}, your registration for {meeting.Title} is confirmed."
        };
        var resp = await _http.PostAsJsonAsync("/notifications/send", body);
        resp.EnsureSuccessStatusCode();
    }
}
