namespace NotificationsAccessor.Infrastructure;

// The "second resource" this Accessor owns. In a real IDesign deployment this would
// wrap an actual SMTP / SendGrid / SES client. Here it just logs to stdout so the
// over-fetched payload is visible in `docker compose logs notifications-accessor`.
public class FakeSmtpGateway
{
    readonly ILogger<FakeSmtpGateway> _log;
    public FakeSmtpGateway(ILogger<FakeSmtpGateway> log) => _log = log;

    public Task SendAsync(string toEmail, string subject, string body, string? rawPayloadJson)
    {
        _log.LogInformation("[FakeSMTP] to={ToEmail} subject={Subject} body={Body} payload={Payload}",
            toEmail, subject, body, rawPayloadJson);
        return Task.CompletedTask;
    }
}
