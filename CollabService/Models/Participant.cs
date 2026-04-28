using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CollabService.Models
{
    /// <summary>
    /// Represents a user who has joined a collaboration session.
    /// Tracks cursor position and assigned color.
    /// </summary>
    public class Participant
    {
        [Key]
        public int ParticipantId { get; set; }

        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Role: HOST, EDITOR, or VIEWER
        /// </summary>
        [Required]
        public string Role { get; set; } = "EDITOR";

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public DateTime? LeftAt { get; set; } = DateTime.Now;

        // Live cursor position broadcast via SignalR
        public int CursorLine { get; set; } = 0;
        public int CursorCol { get; set; } = 0;

        /// <summary>
        /// Unique color assigned to this participant in the session
        /// </summary>
        [Required]
        public string Color { get; set; } = "#000000";

        [JsonIgnore]
        public CollabSession? Session { get; set; }
    }
}