using System.ComponentModel.DataAnnotations;

namespace AuthService.Models
{
    /// <summary>
    /// Represents a user in the CodeSync system.
    /// </summary>
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Role: ADMIN or DEVELOPER
        /// </summary>
        [Required]
        public string Role { get; set; } = "DEVELOPER";

        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Provider: LOCAL, GITHUB, or GOOGLE
        /// </summary>
        public string Provider { get; set; } = "LOCAL";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Bio { get; set; }
    }
}