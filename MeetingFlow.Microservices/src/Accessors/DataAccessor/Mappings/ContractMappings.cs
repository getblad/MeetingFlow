using DataAccessor.Contracts;
using DataAccessor.Models;

namespace DataAccessor.Mappings;

public static class ContractMappings
{
    public static VenueDetailsDto ToDto(this Venue venue) =>
        new(venue.Id, venue.Name, venue.Address, venue.City, venue.Capacity);

    public static MeetingSummaryDto ToSummaryDto(this Meeting meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.Status,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.Venue is null
                ? null
                : new VenueSummaryDto(
                    meeting.Venue.Id,
                    meeting.Venue.Name,
                    meeting.Venue.City,
                    meeting.Venue.Capacity));

    public static MeetingDetailsDto ToDetailsDto(this Meeting meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.Status,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.CreatedAt,
            meeting.UpdatedAt,
            meeting.VenueId,
            meeting.Venue is null
                ? null
                : new VenueDetailsDto(
                    meeting.Venue.Id,
                    meeting.Venue.Name,
                    meeting.Venue.Address,
                    meeting.Venue.City,
                    meeting.Venue.Capacity),
            meeting.Sessions.Select(ToDto).ToList(),
            meeting.Registrations.Select(ToDto).ToList(),
            meeting.Feedback.Select(ToDto).ToList());

    public static AdminMeetingDto ToAdminDto(this Meeting meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Status,
            meeting.InternalNotes,
            meeting.AdminOnlyCode,
            meeting.Venue?.InternalContactName,
            meeting.Venue?.InternalContactPhone);

    public static RegistrationMeetingContextDto ToRegistrationContextDto(this Meeting meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Status,
            meeting.StartsAt,
            meeting.Venue?.Capacity ?? 0);

    public static SessionDto ToDto(this Session session) =>
        new(
            session.Id,
            session.MeetingId,
            session.SpeakerId,
            session.Title,
            session.Description,
            session.StartsAt,
            session.EndsAt,
            session.RoomName,
            session.Speaker is null
                ? null
                : new SpeakerDto(
                    session.Speaker.Id,
                    session.Speaker.FullName,
                    session.Speaker.Bio,
                    session.Speaker.Company));

    public static SpeakerDto ToDto(this Speaker speaker) =>
        new(speaker.Id, speaker.FullName, speaker.Bio, speaker.Company);

    public static AttendeeSummaryDto ToSummaryDto(this Attendee attendee) =>
        new(attendee.Id, attendee.FullName, attendee.Company);

    public static AttendeeContactDto ToContactDto(this Attendee attendee) =>
        new(attendee.Id, attendee.FullName, attendee.Email);

    public static AttendeeDetailsDto ToDetailsDto(this Attendee attendee) =>
        new(
            attendee.Id,
            attendee.FullName,
            attendee.Email,
            attendee.Phone,
            attendee.Company);

    public static RegistrationDto ToDto(this Registration registration) =>
        new(
            registration.Id,
            registration.MeetingId,
            registration.AttendeeId,
            registration.RegisteredAt,
            registration.TicketType,
            registration.PaymentStatus,
            registration.Attendee is null ? null : registration.Attendee.ToSummaryDto());

    public static FeedbackDto ToDto(this Feedback feedback) =>
        new(
            feedback.Id,
            feedback.MeetingId,
            feedback.AttendeeId,
            feedback.Rating,
            feedback.Comment,
            feedback.CreatedAt,
            feedback.Attendee is null ? null : feedback.Attendee.ToSummaryDto());

    public static MeetingTaskDto ToDto(this MeetingTask task) =>
        new(
            task.Id,
            task.MeetingId,
            task.Title,
            task.IsCompleted,
            task.AssignedTo,
            task.CreatedAt,
            task.CompletedAt);
}
