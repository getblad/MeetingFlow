namespace DataAccessor.Contracts;

public sealed record MeetingTaskDto(
    Guid Id,
    Guid MeetingId,
    string Title,
    bool IsCompleted,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record CreateMeetingTaskRequest(
    Guid MeetingId,
    string Title,
    string? AssignedTo);

public sealed record UpdateMeetingTaskRequest(
    string Title,
    bool IsCompleted,
    string? AssignedTo);
