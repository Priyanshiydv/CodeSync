using System.ComponentModel.DataAnnotations;

namespace FileService.DTOs
{
    /// <summary>
    /// Data received to update file content with user attribution.
    /// </summary>
    public class UpdateFileContentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// UserId of person making the edit - for collaborative attribution
        /// </summary>
        [Required]
        public int EditedByUserId { get; set; }
    }
}