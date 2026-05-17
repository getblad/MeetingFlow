using Microsoft.EntityFrameworkCore;
using NotificationsAccessor.Models;

namespace NotificationsAccessor.Data;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e => e.ToTable("notifications", schema: "notifications"));
    }
}
