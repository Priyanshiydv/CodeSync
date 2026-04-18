using System.ComponentModel.DataAnnotations;

namespace FileService.Models
{
    /// <summary>
    /// Represents a code file or folder in a project's directory structure.
    /// Folder entries carry empty content.
    /// </summary>
    public class CodeFile
    {
        [Key]
        public int FileId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full directory path e.g. "src/main/Program.cs"
        /// </summary>
        [Required]
        public string Path { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Full file text content. Empty for folder entries.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; set; } = 0;

        public int CreatedById { get; set; }

        /// <summary>
        /// Tracks who last edited the file for collaborative attribution
        /// </summary>
        public int LastEditedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Soft delete flag - preserves file for potential restoration
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// True if this entry is a folder (no content)
        /// </summary>
        public bool IsFolder { get; set; } = false;
    }
}