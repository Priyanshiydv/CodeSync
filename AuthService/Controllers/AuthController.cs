using AuthService.DTOs;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers
{
    /// <summary>
    /// Handles all authentication and user management HTTP requests.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var user = new User
                {
                    FullName = dto.FullName,
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = dto.Password,
                    Role = "Developer",
                    Provider = "LOCAL"
                };
                var token = await _authService.Register(user);
                return Ok(new { message = "Registration successful!", token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.Login(dto.Email, dto.Password);
                return Ok(new { message = "Login successful!", token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");
            await _authService.Logout(token);
            return Ok(new { message = "Logged out successfully!" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string token)
        {
            try
            {
                var newToken = await _authService.RefreshToken(token);
                return Ok(new { token = newToken });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var user = await _authService.GetUserById(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(
                    ClaimTypes.NameIdentifier)!.Value);
                var token = await _authService.UpdateProfile(userId, dto);
                return Ok(new { message = "Profile updated!", token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(
                    ClaimTypes.NameIdentifier)!.Value);
                await _authService.ChangePassword(userId, dto.NewPassword);
                return Ok(new { message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var users = await _authService.SearchUsers(query);
            return Ok(users);
        }

        [HttpPut("deactivate")]
        [Authorize]
        public async Task<IActionResult> Deactivate([FromQuery] int userId)
        {
            try
            {
                await _authService.DeactivateAccount(userId);
                return Ok(new { message = "Account deactivated!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("roles")]
        public IActionResult GetRoles()
        {
            var roles = new[]
            {
                new { role = "ADMIN", description = "Platform administrator" },
                new { role = "DEVELOPER", description = "Registered developer" }
            };
            return Ok(roles);
        }
    }
}