using System.ComponentModel.DataAnnotations;

namespace CommentService.Models
{
    /// <summary>
    /// Represents an inline code review comment anchored to a specific line.
    /// Supports two-level threading via ParentCommentId.
    /// </summary>
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; } = 0;

        /// <summary>
        /// Null for top-level comments, set for replies
        /// </summary>
        public int? ParentCommentId { get; set; }

        /// <summary>
        /// True when code issue is fixed and comment is addressed
        /// </summary>
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// Links comment to snapshot for contextual stability
        /// </summary>
        public int? SnapshotId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}