namespace Gateway.Contracts;

public sealed record AttendeeSummaryDto(
    Guid Id,
    string FullName,
    string? Company);

public sealed record AttendeeDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Company);

public sealed record CreateAttendeeRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Company);

public sealed record RegistrationDto(
    Guid Id,
    Guid MeetingId,
    Guid AttendeeId,
    DateTimeOffset RegisteredAt,
    string TicketType,
    string PaymentStatus,
    AttendeeSummaryDto? Attendee);

public sealed record CreateRegistrationRequest(
    Guid MeetingId,
    Guid AttendeeId,
    string TicketType);

public sealed record CreateRegistrationResult(
    RegistrationDto Registration,
    decimal CalculatedPrice);

public sealed record SubmitFeedbackRequest(
    Guid MeetingId,
    Guid AttendeeId,
    int Rating,
    string Comment);

public sealed record FeedbackDto(
    Guid Id,
    Guid MeetingId,
    Guid AttendeeId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);
