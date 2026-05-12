using AuthService.Data;
using AuthService.DTOs;
using AuthService.Helpers;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services
{
    /// <summary>
    /// Implements all authentication and user management operations.
    /// </summary>
    public class AuthServiceImpl : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly IUserRepository _userRepository;

        public AuthServiceImpl(
            AuthDbContext context,
            JwtHelper jwtHelper,
            IUserRepository userRepository)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _userRepository = userRepository;
        }

        public async Task<string> Register(User user)
        {
            // Check if email or username already exists
            if (await _userRepository.ExistsByEmail(user.Email))
                throw new Exception("Email already registered!");

            if (await _userRepository.ExistsByUsername(user.Username))
                throw new Exception("Username already taken!");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _jwtHelper.GenerateToken(user);
        }

        public async Task<string> Login(string email, string password)
        {
            var user = await _userRepository.FindByEmail(email)
                ?? throw new Exception("Invalid email or password!");

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new Exception("Invalid email or password!");

            if (!user.IsActive)
                throw new Exception("Account is deactivated!");

            return _jwtHelper.GenerateToken(user);
        }

        public Task Logout(string token)
        {
            // JWT is stateless - client discards token
            return Task.CompletedTask;
        }

        public Task<bool> ValidateToken(string token)
        {
            var isValid = _jwtHelper.ValidateToken(token);
            return Task.FromResult(isValid);
        }

        public Task<string> RefreshToken(string token)
        {
            var newToken = _jwtHelper.RefreshToken(token);
            return Task.FromResult(newToken);
        }

        public async Task<User?> GetUserByEmail(string email) =>
            await _userRepository.FindByEmail(email);

        public async Task<User?> GetUserById(int userId) =>
            await _userRepository.FindByUserId(userId);

        public async Task<string> UpdateProfile(int userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.FindByUserId(userId)
                ?? throw new Exception("User not found!");

            if (dto.FullName != null) user.FullName = dto.FullName;
            if (dto.Username != null) user.Username = dto.Username;
            if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;
            if (dto.Bio != null) user.Bio = dto.Bio;

            await _context.SaveChangesAsync();
            return _jwtHelper.GenerateToken(user);
        }

        public async Task ChangePassword(int userId, string newPassword)
        {
            var user = await _userRepository.FindByUserId(userId)
                ?? throw new Exception("User not found!");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> SearchUsers(string query) =>
            await _userRepository.SearchByUsername(query);

        public async Task DeactivateAccount(int userId)
        {
            var user = await _userRepository.FindByUserId(userId)
                ?? throw new Exception("User not found!");

            user.IsActive = false;
            await _context.SaveChangesAsync();
        }
        public async Task<User?> GetUserByUsername(string username) =>
            await _userRepository.FindByUsername(username);
         }
}