namespace DataAccessor.Contracts;

public sealed record AttendeeSummaryDto(
    Guid Id,
    string FullName,
    string? Company);

public sealed record AttendeeContactDto(
    Guid Id,
    string FullName,
    string Email);

public sealed record AttendeeDetailsDto(
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

public sealed record PersistRegistrationRequest(
    Guid MeetingId,
    Guid AttendeeId,
    string TicketType);

public sealed record FeedbackDto(
    Guid Id,
    Guid MeetingId,
    Guid AttendeeId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt,
    AttendeeSummaryDto? Attendee);

public sealed record PersistFeedbackRequest(
    Guid MeetingId,
    Guid AttendeeId,
    int Rating,
    string Comment);
