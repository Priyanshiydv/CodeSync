using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data
{
    /// <summary>
    /// Database context for Notification Service.
    /// Connects to CodeSync_Notifications database.
    /// </summary>
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(
            DbContextOptions<NotificationDbContext> options)
            : base(options) { }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index for fast recipient queries
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientId, n.IsRead });

            // Index for type based queries
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.Type);
        }
    }
}