using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Interfaces
{
    /// <summary>
    /// Defines all authentication and user management operations.
    /// </summary>
    public interface IAuthService
    {
        Task<string> Register(User user);
        Task<string> Login(string email, string password);
        Task Logout(string token);
        Task<bool> ValidateToken(string token);
        Task<string> RefreshToken(string token);
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserById(int userId);
        Task<string> UpdateProfile(int userId, UpdateProfileDto dto);
        Task ChangePassword(int userId, string newPassword);
        Task<List<User>> SearchUsers(string query);
        Task DeactivateAccount(int userId);
    }
}