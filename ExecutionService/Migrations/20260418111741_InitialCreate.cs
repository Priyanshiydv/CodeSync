using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExecutionService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stdin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Stdout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stderr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExitCode = table.Column<int>(type: "int", nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    MemoryUsedKb = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionJobs", x => x.JobId);
                });

            migrationBuilder.CreateTable(
                name: "SupportedLanguages",
                columns: table => new
                {
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DockerImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportedLanguages", x => x.LanguageId);
                });

            migrationBuilder.InsertData(
                table: "SupportedLanguages",
                columns: new[] { "LanguageId", "DockerImage", "FileExtension", "IsEnabled", "Name", "Version" },
                values: new object[,]
                {
                    { 1, "python:3.11-slim", ".py", true, "Python", "3.11" },
                    { 2, "node:20-slim", ".js", true, "JavaScript", "20.0" },
                    { 3, "openjdk:17-slim", ".java", true, "Java", "17.0" },
                    { 4, "gcc:13-slim", ".c", true, "C", "13.0" },
                    { 5, "gcc:13-slim", ".cpp", true, "C++", "13.0" },
                    { 6, "golang:1.21-slim", ".go", true, "Go", "1.21" },
                    { 7, "rust:1.74-slim", ".rs", true, "Rust", "1.74" },
                    { 8, "node:20-slim", ".ts", true, "TypeScript", "5.0" },
                    { 9, "php:8.2-cli", ".php", true, "PHP", "8.2" },
                    { 10, "ruby:3.2-slim", ".rb", true, "Ruby", "3.2" },
                    { 11, "openjdk:17-slim", ".kt", true, "Kotlin", "1.9" },
                    { 12, "swift:5.9-slim", ".swift", true, "Swift", "5.9" },
                    { 13, "r-base:4.3", ".r", true, "R", "4.3" },
                    { 14, "mcr.microsoft.com/dotnet/sdk:8.0", ".cs", true, "CSharp", "8.0" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_Status",
                table: "ExecutionJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_UserId",
                table: "ExecutionJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionJobs");

            migrationBuilder.DropTable(
                name: "SupportedLanguages");
        }
    }
}
