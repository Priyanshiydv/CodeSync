using Microsoft.EntityFrameworkCore;
using AuthService.Models;

namespace AuthService.Data
{
    /// <summary>
    /// Database context for Auth Service - connects to CodeSync_Auth database.
    /// </summary>
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure no two users can have the same email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}