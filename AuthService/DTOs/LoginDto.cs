using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    /// <summary>
    /// Data received from client during login request.
    /// </summary>
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}