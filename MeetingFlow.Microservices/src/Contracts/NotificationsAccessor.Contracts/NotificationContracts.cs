namespace NotificationsAccessor.Contracts;

public sealed record SendNotificationRequest(
    Guid AttendeeId,
    string RecipientEmail,
    string Channel,
    string Subject,
    string Body);

public sealed record NotificationDto(
    Guid Id,
    Guid AttendeeId,
    string Type,
    string Subject,
    string Body,
    DateTimeOffset? SentAt);
