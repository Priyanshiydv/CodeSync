using System.ComponentModel.DataAnnotations;

namespace CollabService.DTOs
{
    /// <summary>
    /// Data received to update a participant's cursor position.
    /// </summary>
    public class UpdateCursorDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int CursorLine { get; set; }

        [Required]
        public int CursorCol { get; set; }
    }
}