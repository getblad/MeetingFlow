using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Data;

namespace MeetingFlow.Api.Endpoints;

public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this WebApplication app)
    {
        // Educational baseline:
        // This endpoint returns AuditLogEntry entities directly, including TechnicalDetails.
        // TechnicalDetails may contain SQL statements, IP addresses, and other sensitive data.
        // In production, prefer filtering or a dedicated response model.
        app.MapGet("/api/audit-log", async (MeetingFlowDbContext db) =>
        {
            var entries = await db.AuditLogEntries
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Results.Ok(entries);
        });
    }
}
