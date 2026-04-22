using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;

namespace AuthService.Controllers
{
    /// <summary>
    /// Admin-only endpoints for user management.
    /// All endpoints require ADMIN role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN, Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public AdminController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.Provider,
                    u.AvatarUrl,
                    u.Bio
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.Bio,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        // PUT: api/admin/users/{id}/suspend
        [HttpPut("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Don't suspend yourself
            var currentUserId = int.Parse(User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (user.UserId == currentUserId)
                return BadRequest(new { message = "Cannot suspend yourself!" });

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User suspended successfully" });
        }

        // PUT: api/admin/users/{id}/reactivate
        [HttpPut("users/{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User reactivated successfully" });
        }

        // PUT: api/admin/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(
            int id, [FromBody] ChangeRoleRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (request.Role != "DEVELOPER" && request.Role != "ADMIN")
                return BadRequest(new { message = "Invalid role" });

            // Don't change your own role
            var currentUserId = int.Parse(User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (user.UserId == currentUserId)
                return BadRequest(new { message = "Cannot change your own role!" });

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User role changed to {request.Role}" });
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Don't delete yourself
            var currentUserId = int.Parse(User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (user.UserId == currentUserId)
                return BadRequest(new { message = "Cannot delete yourself!" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var adminUsers = await _context.Users.CountAsync(u => u.Role == "ADMIN");
            var developerUsers = await _context.Users.CountAsync(u => u.Role == "DEVELOPER");

            return Ok(new
            {
                totalUsers,
                activeUsers,
                suspendedUsers = totalUsers - activeUsers,
                adminUsers,
                developerUsers
            });
        }
    }

    public class ChangeRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}