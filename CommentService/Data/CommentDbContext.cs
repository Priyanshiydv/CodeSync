using CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Data
{
    /// <summary>
    /// Database context for Comment Service.
    /// Connects to CodeSync_Comments database.
    /// </summary>
    public class CommentDbContext : DbContext
    {
        public CommentDbContext(
            DbContextOptions<CommentDbContext> options)
            : base(options) { }

        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index for fast file comment queries
            modelBuilder.Entity<Comment>()
                .HasIndex(c => new { c.FileId, c.LineNumber });

            // Index for project comment queries
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.ProjectId);
        }
    }
}