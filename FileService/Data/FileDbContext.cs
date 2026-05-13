using FileService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService.Data
{
    /// <summary>
    /// Database context for File Service.
    /// Connects to CodeSync_Files database.
    /// </summary>
    public class FileDbContext : DbContext
    {
        public FileDbContext(DbContextOptions<FileDbContext> options)
            : base(options) { }

        public DbSet<CodeFile> CodeFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique path per project - no two files can have same path
            modelBuilder.Entity<CodeFile>()
                .HasIndex(f => new { f.ProjectId, f.Path })
                .IsUnique();

            // Soft delete filter - automatically excludes deleted files
            modelBuilder.Entity<CodeFile>()
                .HasQueryFilter(f => !f.IsDeleted);
        }
    }
}