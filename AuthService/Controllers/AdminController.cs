using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;
using AuthService.Services;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN, Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AuthDbContext _context;
        // ADD — audit log service
        private readonly AuditLogService _auditLog;

        public AdminController(
            AuthDbContext context,
            AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        // Helper — gets current admin's ID and username
        private int GetAdminId() =>
            int.Parse(User.FindFirst(
                ClaimTypes.NameIdentifier)!.Value);

        private string GetAdminUsername()
        {
            // Try different claim types
            var username = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("username")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(username))
            {
                username = $"Admin#{GetAdminId()}";
            }
            
            return username;
        }

        private string GetIp() =>
            HttpContext.Connection.RemoteIpAddress?
                .ToString() ?? "unknown";

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId, u.Username, u.Email,
                    u.FullName, u.Role, u.IsActive,
                    u.CreatedAt, u.Provider,
                    u.AvatarUrl, u.Bio
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
                    u.UserId, u.Username, u.Email,
                    u.FullName, u.Role, u.IsActive,
                    u.CreatedAt, u.Bio, u.AvatarUrl
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var adminId = GetAdminId();
            if (user.UserId == adminId)
                return BadRequest(new
                {
                    message = "Cannot suspend yourself!"
                });
            var username = user.Username;

            user.IsActive = false;
            await _context.SaveChangesAsync();

            // ADD — audit log
            await _auditLog.LogAsync(
                actorId: adminId,
                actorUsername: GetAdminUsername(),
                action: "SUSPEND_USER",
                entityType: "User",
                entityId: id.ToString(),
                description: $"Suspended user '{user.Username}' (ID: {id})",
                ipAddress: GetIp()
            );

            return Ok(new { message = "User suspended successfully" });
        }

        // PUT: api/admin/users/{id}/reactivate
        [HttpPut("users/{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
                return NotFound(new { message = "User not found" });
            var username = user.Username;
            user.IsActive = true;
            await _context.SaveChangesAsync();

            // ADD — audit log
            await _auditLog.LogAsync(
                actorId: GetAdminId(),
                actorUsername: GetAdminUsername(),
                action: "REACTIVATE_USER",
                entityType: "User",
                entityId: id.ToString(),
                description: $"Reactivated user '{user.Username}' (ID: {id})",
                ipAddress: GetIp()
            );

            return Ok(new { message = "User reactivated successfully" });
        }

        // PUT: api/admin/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(
            int id, [FromBody] ChangeRoleRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId ==id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (request.Role != "DEVELOPER" && request.Role != "ADMIN")
                return BadRequest(new { message = "Invalid role" });

            var adminId = GetAdminId();
            if (user.UserId == adminId)
                return BadRequest(new
                {
                    message = "Cannot change your own role!"
                });

            var oldRole = user.Role;
            user.Role = request.Role;
            await _context.SaveChangesAsync();

            // ADD — audit log
            await _auditLog.LogAsync(
                actorId: adminId,
                actorUsername: GetAdminUsername(),
                action: "CHANGE_USER_ROLE",
                entityType: "User",
                entityId: id.ToString(),
                description: $"Changed '{user.Username}' role from {oldRole} to {request.Role}",
                ipAddress: GetIp()
            );

            return Ok(new
            {
                message = $"User role changed to {request.Role}"
            });
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var adminId = GetAdminId();
            if (user.UserId == adminId)
                return BadRequest(new
                {
                    message = "Cannot delete yourself!"
                });

            var username = user.Username;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // ADD — audit log
            await _auditLog.LogAsync(
                actorId: adminId,
                actorUsername: GetAdminUsername(),
                action: "DELETE_USER",
                entityType: "User",
                entityId: id.ToString(),
                description: $"Permanently deleted user '{username}' (ID: {id})",
                ipAddress: GetIp()
            );

            return Ok(new { message = "User deleted successfully" });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users
                .CountAsync(u => u.IsActive);
            var adminUsers = await _context.Users
                .CountAsync(u => u.Role == "ADMIN");
            var developerUsers = await _context.Users
                .CountAsync(u => u.Role == "DEVELOPER");

            return Ok(new
            {
                totalUsers,
                activeUsers,
                suspendedUsers = totalUsers - activeUsers,
                adminUsers,
                developerUsers
            });
        }

        // ADD — GET: api/admin/audit-logs
        // Returns paginated audit log entries
        // Case study §2.4 — admin views audit logs
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var logs = await _auditLog.GetAllAsync(page, pageSize);
            return Ok(logs);
        }

        // ADD — GET: api/admin/audit-logs/actor/{actorId}
        [HttpGet("audit-logs/actor/{actorId}")]
        public async Task<IActionResult> GetByActor(int actorId)
        {
            var logs = await _auditLog.GetByActorAsync(actorId);
            return Ok(logs);
        }

        // ADD — GET: api/admin/audit-logs/entity/{type}/{id}
        [HttpGet("audit-logs/entity/{entityType}/{entityId}")]
        public async Task<IActionResult> GetByEntity(
            string entityType, string entityId)
        {
            var logs = await _auditLog
                .GetByEntityAsync(entityType, entityId);
            return Ok(logs);
        }
    }

    public class ChangeRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}