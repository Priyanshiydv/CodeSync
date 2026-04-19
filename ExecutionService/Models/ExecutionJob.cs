using System.ComponentModel.DataAnnotations;

namespace ExecutionService.Models
{
    /// <summary>
    /// Represents a code execution job submitted to a Docker sandbox container.
    /// Tracks source, output, status and performance metrics.
    /// </summary>
    public class ExecutionJob
    {
        [Key]
        public Guid JobId { get; set; } = Guid.NewGuid();

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Language { get; set; } = string.Empty;

        [Required]
        public string SourceCode { get; set; } = string.Empty;

        // Custom standard input for interactive programs
        public string? Stdin { get; set; }

        /// <summary>
        /// Status: QUEUED, RUNNING, COMPLETED, FAILED, TIMED_OUT, CANCELLED
        /// </summary>
        [Required]
        public string Status { get; set; } = "QUEUED";

        // Output streams from container
        public string? Stdout { get; set; }
        public string? Stderr { get; set; }

        public int? ExitCode { get; set; }

        // Performance metrics
        public long? ExecutionTimeMs { get; set; }
        public long? MemoryUsedKb { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}