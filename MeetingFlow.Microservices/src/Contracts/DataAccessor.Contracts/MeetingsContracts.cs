namespace DataAccessor.Contracts;

public sealed record VenueSummaryDto(
    Guid Id,
    string Name,
    string City,
    int Capacity);

public sealed record VenueDetailsDto(
    Guid Id,
    string Name,
    string Address,
    string City,
    int Capacity);

public sealed record CreateVenueRequest(
    string Name,
    string Address,
    string City,
    int Capacity);

public sealed record SpeakerDto(
    Guid Id,
    string FullName,
    string Bio,
    string? Company);

public sealed record SessionDto(
    Guid Id,
    Guid MeetingId,
    Guid SpeakerId,
    string Title,
    string Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string RoomName,
    SpeakerDto? Speaker);

public sealed record MeetingSummaryDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    VenueSummaryDto? Venue);

public sealed record MeetingDetailsDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid VenueId,
    VenueDetailsDto? Venue,
    IReadOnlyList<SessionDto> Sessions,
    IReadOnlyList<RegistrationDto> Registrations,
    IReadOnlyList<FeedbackDto> Feedback);

public sealed record AdminMeetingDto(
    Guid Id,
    string Title,
    string Status,
    string? InternalNotes,
    string? AdminOnlyCode,
    string? VenueInternalContactName,
    string? VenueInternalContactPhone);

public sealed record RegistrationMeetingContextDto(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset StartsAt,
    int VenueCapacity);

public sealed record UpdateMeetingRequest(
    string Title,
    string Description,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid VenueId);

public sealed record CreateMeetingRequest(
    string Title,
    string Description,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid VenueId);
