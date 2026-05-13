using System.ComponentModel.DataAnnotations;

namespace VersionService.Models
{
    /// <summary>
    /// Represents a point-in-time capture of a file's content.
    /// Analogous to a Git commit with SHA-256 integrity hash.
    /// </summary>
    public class Snapshot
    {
        [Key]
        public int SnapshotId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        /// <summary>
        /// Commit message describing what changed in this snapshot
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Full file text content at this point in time
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 hash for integrity verification on restore
        /// </summary>
        [Required]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Links to parent snapshot forming the history chain (DAG)
        /// Null for first snapshot in a branch
        /// </summary>
        public int? ParentSnapshotId { get; set; }

        /// <summary>
        /// Branch this snapshot belongs to. Default is 'main'
        /// </summary>
        [Required]
        public string Branch { get; set; } = "main";

        /// <summary>
        /// Optional release tag e.g. v1.0.0
        /// </summary>
        public string? Tag { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}