using System.ComponentModel.DataAnnotations;

namespace CollabService.DTOs
{
    /// <summary>
    /// Data received when a user joins a session.
    /// </summary>
    public class JoinSessionDto
    {
        [Required]
        public int UserId { get; set; }

        public string? SessionPassword { get; set; }
    }
}