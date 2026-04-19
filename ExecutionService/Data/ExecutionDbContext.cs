using ExecutionService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Data
{
    /// <summary>
    /// Database context for Execution Service.
    /// Connects to CodeSync_Execution database.
    /// </summary>
    public class ExecutionDbContext : DbContext
    {
        public ExecutionDbContext(
            DbContextOptions<ExecutionDbContext> options)
            : base(options) { }

        public DbSet<ExecutionJob> ExecutionJobs { get; set; }
        public DbSet<SupportedLanguage> SupportedLanguages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index for fast status queries
            modelBuilder.Entity<ExecutionJob>()
                .HasIndex(j => j.Status);

            // Index for user history queries
            modelBuilder.Entity<ExecutionJob>()
                .HasIndex(j => j.UserId);

            // Seed supported languages with Docker images
            modelBuilder.Entity<SupportedLanguage>().HasData(
                new SupportedLanguage
                {
                    LanguageId = 1, Name = "Python",
                    DockerImage = "python:3.11-slim",
                    Version = "3.11", FileExtension = ".py"
                },
                new SupportedLanguage
                {
                    LanguageId = 2, Name = "JavaScript",
                    DockerImage = "node:20-slim",
                    Version = "20.0", FileExtension = ".js"
                },
                new SupportedLanguage
                {
                    LanguageId = 3, Name = "Java",
                    DockerImage = "openjdk:17-slim",
                    Version = "17.0", FileExtension = ".java"
                },
                new SupportedLanguage
                {
                    LanguageId = 4, Name = "C",
                    DockerImage = "gcc:13-slim",
                    Version = "13.0", FileExtension = ".c"
                },
                new SupportedLanguage
                {
                    LanguageId = 5, Name = "C++",
                    DockerImage = "gcc:13-slim",
                    Version = "13.0", FileExtension = ".cpp"
                },
                new SupportedLanguage
                {
                    LanguageId = 6, Name = "Go",
                    DockerImage = "golang:1.21-slim",
                    Version = "1.21", FileExtension = ".go"
                },
                new SupportedLanguage
                {
                    LanguageId = 7, Name = "Rust",
                    DockerImage = "rust:1.74-slim",
                    Version = "1.74", FileExtension = ".rs"
                },
                new SupportedLanguage
                {
                    LanguageId = 8, Name = "TypeScript",
                    DockerImage = "node:20-slim",
                    Version = "5.0", FileExtension = ".ts"
                },
                new SupportedLanguage
                {
                    LanguageId = 9, Name = "PHP",
                    DockerImage = "php:8.2-cli",
                    Version = "8.2", FileExtension = ".php"
                },
                new SupportedLanguage
                {
                    LanguageId = 10, Name = "Ruby",
                    DockerImage = "ruby:3.2-slim",
                    Version = "3.2", FileExtension = ".rb"
                },
                new SupportedLanguage
                {
                    LanguageId = 11, Name = "Kotlin",
                    DockerImage = "openjdk:17-slim",
                    Version = "1.9", FileExtension = ".kt"
                },
                new SupportedLanguage
                {
                    LanguageId = 12, Name = "Swift",
                    DockerImage = "swift:5.9-slim",
                    Version = "5.9", FileExtension = ".swift"
                },
                new SupportedLanguage
                {
                    LanguageId = 13, Name = "R",
                    DockerImage = "r-base:4.3",
                    Version = "4.3", FileExtension = ".r"
                },
                new SupportedLanguage
                {
                    LanguageId = 14, Name = "CSharp",
                    DockerImage = "mcr.microsoft.com/dotnet/sdk:8.0",
                    Version = "8.0", FileExtension = ".cs"
                }

            );
        }
    }
}