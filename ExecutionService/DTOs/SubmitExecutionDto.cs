using System.ComponentModel.DataAnnotations;

namespace ExecutionService.DTOs
{
    /// <summary>
    /// Data received to submit a new code execution job.
    /// </summary>
    public class SubmitExecutionDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public string Language { get; set; } = string.Empty;

        [Required]
        public string SourceCode { get; set; } = string.Empty;

        // Optional custom stdin for interactive programs
        public string? Stdin { get; set; }
    }
}