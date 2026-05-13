using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models
{
    /// <summary>
    /// Represents an in-app or email notification for a collaboration event.
    /// Carries deep-link info via RelatedId and RelatedType.
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int RecipientId { get; set; }

        [Required]
        public int ActorId { get; set; }

        /// <summary>
        /// Type: SESSION_INVITE, COMMENT, MENTION, SNAPSHOT, FORK
        /// </summary>
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        // ID of related entity (SessionId, CommentId, SnapshotId etc)
        public string? RelatedId { get; set; }

        // Type of related entity for deep linking
        public string? RelatedType { get; set; }
        public string? DeepLinkUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}