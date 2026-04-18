using System.ComponentModel.DataAnnotations;

namespace CollabService.DTOs
{
    /// <summary>
    /// Data received to create a new collaboration session.
    /// </summary>
    public class CreateSessionDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public string Language { get; set; } = string.Empty;

        public int MaxParticipants { get; set; } = 10;

        public bool IsPasswordProtected { get; set; } = false;

        public string? SessionPassword { get; set; }
    }
}