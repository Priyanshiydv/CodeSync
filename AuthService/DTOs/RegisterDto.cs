using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MinLength(3)]
        [MaxLength(30)]
        [RegularExpression("^[a-zA-Z0-9_]+$",
            ErrorMessage = "Username can only contain letters, numbers and underscores")]
        public string Username { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

    
        [Required]
        public string Role { get; set; } = "Developer";
    }
}