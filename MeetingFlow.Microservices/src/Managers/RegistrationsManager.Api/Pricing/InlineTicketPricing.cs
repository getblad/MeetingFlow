using RegistrationsManager.Models;

namespace RegistrationsManager.Pricing;

// Intentionally NOT a separate Engine service. The pricing rules live inside the Manager
// and operate on the full Meeting + Registration entities even though only TicketType,
// Status, and StartsAt matter. Refactoring this into a real PricingEngine.Api with a
// narrow request shape is the bonus exercise.
public static class InlineTicketPricing
{
    public static decimal CalculatePrice(Meeting meeting, Registration registration)
    {
        var basePrice = registration.TicketType switch
        {
            "VIP" => 499m,
            "Early Bird" => 99m,
            "Student" => 49m,
            "General" => 199m,
            _ => 149m
        };

        if (meeting.Status == "Cancelled") return 0m;

        var daysUntil = (meeting.StartsAt - DateTimeOffset.UtcNow).TotalDays;
        if (daysUntil < 7) basePrice *= 1.15m;
        else if (daysUntil > 60) basePrice *= 0.90m;

        return Math.Round(basePrice, 2);
    }
}
