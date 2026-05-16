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
        app.MapPost("/api/registrations", async (CreateRegistrationRequest request, MeetingFlowDbContext db) =>
        {
            var attendee = await db.Attendees.FirstOrDefaultAsync(a => a.Email == request.AttendeeEmail);
            if (attendee is null)
            {
                attendee = new Attendee { Id = Guid.NewGuid(), FullName = request.AttendeeName, Email = request.AttendeeEmail };
                db.Attendees.Add(attendee);
            }

            var registration = new Registration
            {
                Id = Guid.NewGuid(),
                MeetingId = request.MeetingId,
                AttendeeId = attendee.Id,
                TicketType = request.TicketType,
                RegisteredAt = DateTimeOffset.UtcNow,
                PaymentStatus = "Pending",
            };

            db.Registrations.Add(registration);
            await db.SaveChangesAsync();

            return Results.Created($"/api/registrations/{registration.Id}", registration);
        });
    }
}

record CreateRegistrationRequest(Guid MeetingId, string AttendeeName, string AttendeeEmail, string TicketType);
