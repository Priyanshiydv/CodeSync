using AuthService.Data;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories
{
    /// <summary>
    /// Handles all database operations for User entity.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;

        public UserRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<User?> FindByEmail(string email) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> FindByUsername(string username) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        public async Task<User?> FindByUserId(int userId) =>
            await _context.Users.FindAsync(userId);

        public async Task<bool> ExistsByEmail(string email) =>
            await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<bool> ExistsByUsername(string username) =>
            await _context.Users.AnyAsync(u => u.Username == username);

        public async Task<List<User>> FindAllByRole(string role) =>
            await _context.Users.Where(u => u.Role == role).ToListAsync();

        public async Task<List<User>> SearchByUsername(string username) =>
            await _context.Users
                .Where(u => u.Username.Contains(username))
                .ToListAsync();

        public async Task DeleteByUserId(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}