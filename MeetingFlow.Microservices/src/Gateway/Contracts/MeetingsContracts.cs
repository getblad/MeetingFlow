namespace Gateway.Contracts;

public sealed record VenueDto(
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
    Guid SpeakerId,
    string Title,
    string Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string RoomName,
    SpeakerDto? Speaker);

public sealed record MeetingListItemDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? VenueCity);

public sealed record MeetingDetailsDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    VenueDto? Venue,
    IReadOnlyList<SessionDto> Sessions,
    int RegistrationCount,
    double? AverageRating);

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
