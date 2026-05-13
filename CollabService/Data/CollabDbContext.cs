using CollabService.Models;
using Microsoft.EntityFrameworkCore;

namespace CollabService.Data
{
    /// <summary>
    /// Database context for Collab Service.
    /// Connects to CodeSync_Collab database.
    /// </summary>
    public class CollabDbContext : DbContext
    {
        public CollabDbContext(DbContextOptions<CollabDbContext> options)
            : base(options) { }

        public DbSet<CollabSession> CollabSessions { get; set; }
        public DbSet<Participant> Participants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Session -> Participants relationship
            modelBuilder.Entity<Participant>()
                .HasOne(p => p.Session)
                .WithMany(s => s.Participants)
                .HasForeignKey(p => p.SessionId);

            // One user can only join a session once
            modelBuilder.Entity<Participant>()
                .HasIndex(p => new { p.SessionId, p.UserId })
                .IsUnique();
        }
    }
}