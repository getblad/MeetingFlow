namespace RegistrationsManager.Mappings;

public static class ContractMappings
{
    public static RegistrationsManager.Contracts.AttendeeDto ToManagerDto(
        this DataAccessor.Contracts.AttendeeDetailsDto attendee) =>
        new(
            attendee.Id,
            attendee.FullName,
            attendee.Email,
            attendee.Phone,
            attendee.Company);

    public static RegistrationsManager.Contracts.RegistrationDto ToManagerDto(
        this DataAccessor.Contracts.RegistrationDto registration) =>
        new(
            registration.Id,
            registration.MeetingId,
            registration.AttendeeId,
            registration.RegisteredAt,
            registration.TicketType,
            registration.PaymentStatus,
            registration.Attendee is null
                ? null
                : new RegistrationsManager.Contracts.AttendeeSummaryDto(
                    registration.Attendee.Id,
                    registration.Attendee.FullName,
                    registration.Attendee.Company));

    public static RegistrationsManager.Contracts.FeedbackDto ToManagerDto(
        this DataAccessor.Contracts.FeedbackDto feedback) =>
        new(
            feedback.Id,
            feedback.MeetingId,
            feedback.AttendeeId,
            feedback.Rating,
            feedback.Comment,
            feedback.CreatedAt);
}
