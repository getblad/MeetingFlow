using DataAccessor.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessor.Data;

public class MeetingFlowDbContext : DbContext
{
    public MeetingFlowDbContext(DbContextOptions<MeetingFlowDbContext> options) : base(options) { }

    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meeting>(e =>
        {
            e.ToTable("meetings", schema: "meetings");
            e.HasOne(x => x.Venue).WithMany(v => v.Meetings).HasForeignKey(x => x.VenueId);
            e.HasMany(x => x.Sessions).WithOne(s => s.Meeting).HasForeignKey(s => s.MeetingId);
            e.HasMany(x => x.Registrations).WithOne(r => r.Meeting).HasForeignKey(r => r.MeetingId);
            e.HasMany(x => x.Feedback).WithOne(f => f.Meeting).HasForeignKey(f => f.MeetingId);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.ToTable("sessions", schema: "meetings");
            e.HasOne(x => x.Speaker).WithMany(s => s.Sessions).HasForeignKey(x => x.SpeakerId);
        });

        modelBuilder.Entity<Speaker>(e => e.ToTable("speakers", schema: "meetings"));
        modelBuilder.Entity<Venue>(e => e.ToTable("venues", schema: "meetings"));

        modelBuilder.Entity<Attendee>(e => e.ToTable("attendees", schema: "registrations"));
        modelBuilder.Entity<Registration>(e =>
        {
            e.ToTable("registrations", schema: "registrations");
            e.HasOne(x => x.Attendee).WithMany(a => a.Registrations).HasForeignKey(x => x.AttendeeId);
        });

        modelBuilder.Entity<Feedback>(e =>
        {
            e.ToTable("feedback", schema: "feedback");
            e.HasOne(x => x.Attendee).WithMany(a => a.Feedback).HasForeignKey(x => x.AttendeeId);
        });

        modelBuilder.Entity<AuditLogEntry>(e => e.ToTable("audit_log", schema: "meetings"));
    }
}
