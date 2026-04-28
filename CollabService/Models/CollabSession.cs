using System.ComponentModel.DataAnnotations;

namespace CollabService.Models
{
    /// <summary>
    /// Represents a real-time collaboration session bound to a specific file.
    /// </summary>
    public class CollabSession
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public int OwnerId { get; set; }

        /// <summary>
        /// Status: ACTIVE or ENDED
        /// </summary>
        [Required]
        public string Status { get; set; } = "ACTIVE";

        [Required]
        public string Language { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? EndedAt { get; set; } = DateTime.Now;

        public int MaxParticipants { get; set; } = 10;

        public bool IsPasswordProtected { get; set; } = false;

        public string? SessionPassword { get; set; }
        // Tracks last participant activity for idle cleanup
        public DateTime LastActivityAt { get; set; } = DateTime.Now;

        // Navigation property
        public ICollection<Participant> Participants { get; set; } 
            = new List<Participant>();
    }
}