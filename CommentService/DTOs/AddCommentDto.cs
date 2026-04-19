using System.ComponentModel.DataAnnotations;

namespace CommentService.DTOs
{
    /// <summary>
    /// Data received to add a new inline comment.
    /// </summary>
    public class AddCommentDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; } = 0;

        // Null for top-level, set for replies
        public int? ParentCommentId { get; set; }

        public int? SnapshotId { get; set; }
    }
}