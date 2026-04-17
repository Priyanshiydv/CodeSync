namespace AuthService.DTOs
{
    /// <summary>
    /// Data received from client to update user profile.
    /// </summary>
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }
}