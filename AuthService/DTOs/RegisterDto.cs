using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Allowed values: Admin, Developer, Guest
        /// </summary>
        [Required]
        [RegularExpression("^(Admin|Developer|Guest)$",
            ErrorMessage = "Role must be Admin, Developer, or Guest")]
        public string Role { get; set; } = "Developer";
    }
}