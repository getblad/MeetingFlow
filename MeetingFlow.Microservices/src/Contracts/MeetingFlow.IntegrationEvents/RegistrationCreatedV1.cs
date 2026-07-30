namespace MeetingFlow.IntegrationEvents;

public sealed record RegistrationCreatedV1(
    Guid EventId,
    Guid RegistrationId,
    Guid MeetingId,
    Guid AttendeeId,
    string MeetingTitle,
    string RecipientName,
    string RecipientEmail,
    DateTimeOffset RegisteredAt);
