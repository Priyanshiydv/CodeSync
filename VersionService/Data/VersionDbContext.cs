using Microsoft.EntityFrameworkCore;
using VersionService.Models;

namespace VersionService.Data
{
    /// <summary>
    /// Database context for Version Service.
    /// Connects to CodeSync_Versions database.
    /// </summary>
    public class VersionDbContext : DbContext
    {
        public VersionDbContext(DbContextOptions<VersionDbContext> options)
            : base(options) { }

        public DbSet<Snapshot> Snapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index for fast file history queries
            modelBuilder.Entity<Snapshot>()
                .HasIndex(s => new { s.FileId, s.Branch });

            // Index for hash lookup on restore integrity check
            modelBuilder.Entity<Snapshot>()
                .HasIndex(s => s.Hash);
        }
    }
}