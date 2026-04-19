using System.ComponentModel.DataAnnotations;

namespace NotificationService.DTOs
{
    /// <summary>
    /// Data received to send a single notification.
    /// </summary>
    public class SendNotificationDto
    {
        [Required]
        public int RecipientId { get; set; }

        [Required]
        public int ActorId { get; set; }

        /// <summary>
        /// Type: SESSION_INVITE, COMMENT, MENTION, SNAPSHOT, FORK
        /// </summary>
        [Required]
        [RegularExpression(
            "^(SESSION_INVITE|COMMENT|MENTION|SNAPSHOT|FORK)$",
            ErrorMessage =
                "Type must be SESSION_INVITE, COMMENT, MENTION, SNAPSHOT or FORK")]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? RelatedId { get; set; }
        public string? RelatedType { get; set; }
    }
}