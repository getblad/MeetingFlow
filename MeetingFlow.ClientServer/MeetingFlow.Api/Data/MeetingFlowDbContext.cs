using Microsoft.EntityFrameworkCore;
using MeetingFlow.Api.Models;

namespace MeetingFlow.Api.Data;

public class MeetingFlowDbContext : DbContext
{
    public MeetingFlowDbContext(DbContextOptions<MeetingFlowDbContext> options) : base(options) { }

    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meeting>(e =>
        {
            e.HasOne(x => x.Venue).WithMany(v => v.Meetings).HasForeignKey(x => x.VenueId);
            e.HasMany(x => x.Sessions).WithOne(s => s.Meeting).HasForeignKey(s => s.MeetingId);
            e.HasMany(x => x.Registrations).WithOne(r => r.Meeting).HasForeignKey(r => r.MeetingId);
            e.HasMany(x => x.Feedback).WithOne(f => f.Meeting).HasForeignKey(f => f.MeetingId);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasOne(x => x.Speaker).WithMany(s => s.Sessions).HasForeignKey(x => x.SpeakerId);
        });

        modelBuilder.Entity<Registration>(e =>
        {
            e.HasOne(x => x.Attendee).WithMany(a => a.Registrations).HasForeignKey(x => x.AttendeeId);
        });

        modelBuilder.Entity<Feedback>(e =>
        {
            e.HasOne(x => x.Attendee).WithMany(a => a.Feedback).HasForeignKey(x => x.AttendeeId);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(x => x.Attendee).WithMany(a => a.Notifications).HasForeignKey(x => x.AttendeeId);
        });
    }
}
