namespace NotificationService.DTOs
{
    // ADD — DTO for incoming @mention notification requests
    // called by CommentService when @username is detected in a comment
    public class MentionNotificationDto
    {
        public int ActorId { get; set; }
        public string Type { get; set; } = "MENTION";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RelatedId { get; set; } = string.Empty;
        public string RelatedType { get; set; } = string.Empty;
        public string DeepLinkUrl { get; set; } = string.Empty;
        public string RecipientUsername { get; set; } = string.Empty;
    }
}