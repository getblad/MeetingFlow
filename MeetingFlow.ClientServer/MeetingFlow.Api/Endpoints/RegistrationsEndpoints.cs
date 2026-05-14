using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Data;
using MeetingFlow.Api.Models;

namespace MeetingFlow.Api.Endpoints;

public static class RegistrationsEndpoints
{
    public static void MapRegistrationsEndpoints(this WebApplication app)
    {
        // Educational baseline:
        // This endpoint accepts the Registration entity directly from the request body.
        // The client can send fields it should not control (e.g., InternalPaymentReference).
        // In production, prefer a dedicated CreateRegistrationRequest.
        app.MapPost("/api/registrations", async (Registration registration, MeetingFlowDbContext db) =>
        {
            registration.Id = Guid.NewGuid();
            registration.RegisteredAt = DateTimeOffset.UtcNow;
            registration.PaymentStatus = "Pending";

            db.Registrations.Add(registration);
            await db.SaveChangesAsync();

            return Results.Created($"/api/registrations/{registration.Id}", registration);
        });
    }
}
