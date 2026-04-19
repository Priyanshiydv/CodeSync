using System.ComponentModel.DataAnnotations;

namespace NotificationService.DTOs
{
    /// <summary>
    /// Data received to send an email notification via MailKit.
    /// </summary>
    public class SendEmailDto
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;
    }
}