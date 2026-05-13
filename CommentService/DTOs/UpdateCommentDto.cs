using System.ComponentModel.DataAnnotations;

namespace CommentService.DTOs
{
    /// <summary>
    /// Data received to update an existing comment.
    /// </summary>
    public class UpdateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}