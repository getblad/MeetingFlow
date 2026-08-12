namespace Gateway.Mappings;

public static class PublicMappings
{
    public static Gateway.Contracts.VenueDto ToPublicDto(
        this MeetingsManager.Contracts.VenueDto venue) =>
        new(venue.Id, venue.Name, venue.Address, venue.City, venue.Capacity);

    public static Gateway.Contracts.MeetingListItemDto ToPublicDto(
        this MeetingsManager.Contracts.MeetingListItemDto meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.Status,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.VenueName,
            meeting.VenueCity);

    public static Gateway.Contracts.MeetingDetailsDto ToPublicDto(
        this MeetingsManager.Contracts.MeetingDetailsDto meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.Status,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.Venue is null
                ? null
                : new Gateway.Contracts.VenueDto(
                    meeting.Venue.Id,
                    meeting.Venue.Name,
                    meeting.Venue.Address,
                    meeting.Venue.City,
                    meeting.Venue.Capacity),
            meeting.Sessions.Select(ToPublicDto).ToList(),
            meeting.RegistrationCount,
            meeting.AverageRating);

    public static Gateway.Contracts.SessionDto ToPublicDto(
        this MeetingsManager.Contracts.SessionDto session) =>
        new(
            session.Id,
            session.SpeakerId,
            session.Title,
            session.Description,
            session.StartsAt,
            session.EndsAt,
            session.RoomName,
            session.Speaker is null ? null : session.Speaker.ToPublicDto());

    public static Gateway.Contracts.SpeakerDto ToPublicDto(
        this MeetingsManager.Contracts.SpeakerDto speaker) =>
        new(speaker.Id, speaker.FullName, speaker.Bio, speaker.Company);

    public static Gateway.Contracts.RegistrationDto ToPublicDto(
        this RegistrationsManager.Contracts.RegistrationDto registration) =>
        new(
            registration.Id,
            registration.MeetingId,
            registration.AttendeeId,
            registration.RegisteredAt,
            registration.TicketType,
            registration.PaymentStatus,
            registration.Attendee is null
                ? null
                : new Gateway.Contracts.AttendeeSummaryDto(
                    registration.Attendee.Id,
                    registration.Attendee.FullName,
                    registration.Attendee.Company));

    public static Gateway.Contracts.AttendeeDto ToPublicDto(
        this RegistrationsManager.Contracts.AttendeeDto attendee) =>
        new(
            attendee.Id,
            attendee.FullName,
            attendee.Email,
            attendee.Phone,
            attendee.Company);

    public static Gateway.Contracts.CreateRegistrationResult ToPublicDto(
        this RegistrationsManager.Contracts.CreateRegistrationResult result) =>
        new(result.Registration.ToPublicDto(), result.CalculatedPrice);

    public static Gateway.Contracts.FeedbackDto ToPublicDto(
        this RegistrationsManager.Contracts.FeedbackDto feedback) =>
        new(
            feedback.Id,
            feedback.MeetingId,
            feedback.AttendeeId,
            feedback.Rating,
            feedback.Comment,
            feedback.CreatedAt);

    public static Gateway.Contracts.ChatResult ToPublicDto(
        this AiChatEngine.Contracts.ChatResult result) =>
        new(
            result.Reply,
            result.Action is null
                ? null
                : new Gateway.Contracts.ChatActionDto(
                    result.Action.Type,
                    result.Action.Parameters),
            result.Data);
}
