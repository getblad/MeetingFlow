namespace SchedulingEngine.Contracts;

public sealed record SessionSlotDto(
    Guid Id,
    string RoomName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

public sealed record CheckConflictRequest(
    SessionSlotDto Candidate,
    IReadOnlyList<SessionSlotDto> Existing);

public sealed record CheckConflictResult(bool HasConflict);

public sealed record CheckCapacityRequest(
    int VenueCapacity,
    int CurrentRegistrationCount);

public sealed record CheckCapacityResult(
    bool HasCapacity,
    int AvailablePlaces);
