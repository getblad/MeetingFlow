using DataAccessor.Contracts;
using MeetingsManager.Contracts;

namespace MeetingsManager.Mappings;

public static class ContractMappings
{
    public static MeetingsManager.Contracts.VenueDto ToManagerDto(
        this DataAccessor.Contracts.VenueDetailsDto venue) =>
        new(venue.Id, venue.Name, venue.Address, venue.City, venue.Capacity);

    public static MeetingListItemDto ToManagerDto(this MeetingSummaryDto meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.Status,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.Venue?.Name,
            meeting.Venue?.City);

    public static MeetingsManager.Contracts.MeetingDetailsDto ToManagerDto(
        this DataAccessor.Contracts.MeetingDetailsDto meeting)
    {
        double? averageRating = meeting.Feedback.Count == 0
            ? null
            : meeting.Feedback.Average(feedback => feedback.Rating);

        return new MeetingsManager.Contracts.MeetingDetailsDto(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.Status,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.Venue is null
                ? null
                : new MeetingsManager.Contracts.VenueDto(
                    meeting.Venue.Id,
                    meeting.Venue.Name,
                    meeting.Venue.Address,
                    meeting.Venue.City,
                    meeting.Venue.Capacity),
            meeting.Sessions.Select(ToManagerDto).ToList(),
            meeting.Registrations.Count,
            averageRating);
    }

    public static MeetingsManager.Contracts.SessionDto ToManagerDto(
        this DataAccessor.Contracts.SessionDto session) =>
        new(
            session.Id,
            session.SpeakerId,
            session.Title,
            session.Description,
            session.StartsAt,
            session.EndsAt,
            session.RoomName,
            session.Speaker is null ? null : session.Speaker.ToManagerDto());

    public static MeetingsManager.Contracts.SpeakerDto ToManagerDto(
        this DataAccessor.Contracts.SpeakerDto speaker) =>
        new(speaker.Id, speaker.FullName, speaker.Bio, speaker.Company);

    public static MeetingsManager.Contracts.AdminMeetingDto ToManagerDto(
        this DataAccessor.Contracts.AdminMeetingDto meeting) =>
        new(
            meeting.Id,
            meeting.Title,
            meeting.Status,
            meeting.InternalNotes,
            meeting.AdminOnlyCode,
            meeting.VenueInternalContactName,
            meeting.VenueInternalContactPhone);
}
