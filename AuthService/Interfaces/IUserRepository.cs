using AuthService.Models;

namespace AuthService.Interfaces
{
    /// <summary>
    /// Repository interface for User data access operations.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> FindByEmail(string email);
        Task<User?> FindByUsername(string username);
        Task<User?> FindByUserId(int userId);
        Task<bool> ExistsByEmail(string email);
        Task<bool> ExistsByUsername(string username);
        Task<List<User>> FindAllByRole(string role);
        Task<List<User>> SearchByUsername(string username);
        Task DeleteByUserId(int userId);
    }
}