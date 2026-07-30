using DataAccessor.Contracts;

namespace RegistrationsManager.Pricing;

public static class InlineTicketPricing
{
    public static decimal CalculatePrice(
        RegistrationMeetingContextDto meeting,
        string ticketType,
        DateTimeOffset now)
    {
        var basePrice = ticketType switch
        {
            "VIP" => 499m,
            "Early Bird" => 99m,
            "Student" => 49m,
            "General" => 199m,
            _ => 149m
        };

        if (meeting.Status == "Cancelled") return 0m;

        var daysUntil = (meeting.StartsAt - now).TotalDays;
        if (daysUntil < 7) basePrice *= 1.15m;
        else if (daysUntil > 60) basePrice *= 0.90m;

        return Math.Round(basePrice, 2);
    }
}
