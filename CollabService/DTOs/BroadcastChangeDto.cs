using System.ComponentModel.DataAnnotations;

namespace CollabService.DTOs
{
    /// <summary>
    /// Data received to broadcast a code change to all session participants.
    /// </summary>
    public class BroadcastChangeDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int CursorLine { get; set; }
        public int CursorCol { get; set; }
    }
}