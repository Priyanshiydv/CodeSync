using System.ComponentModel.DataAnnotations;

namespace VersionService.DTOs
{
    /// <summary>
    /// Data received to create a new snapshot of a file.
    /// </summary>
    public class CreateSnapshotDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Defaults to main branch if not specified
        public string Branch { get; set; } = "main";

        public int? ParentSnapshotId { get; set; }
    }
}