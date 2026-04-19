using System.ComponentModel.DataAnnotations;

namespace NotificationService.DTOs
{
    /// <summary>
    /// Data received for admin bulk notification broadcast.
    /// </summary>
    public class SendBulkNotificationDto
    {
        [Required]
        public int ActorId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        // If empty sends to all users
        public List<int> RecipientIds { get; set; } = new();

        public string Type { get; set; } = "COMMENT";
    }
}