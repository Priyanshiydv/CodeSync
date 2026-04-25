using AuthService.Helpers;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class OAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;

        public OAuthController(
            IAuthService authService,
            IUserRepository userRepository,
            JwtHelper jwtHelper)
        {
            _authService = authService;
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
        }

        // ─── Google OAuth2 ─────────────────────────────────────────────────

        // Step 1 — frontend redirects user here to start Google login
        [HttpGet("google/login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                // After Google auth, come back to our callback
                RedirectUri = "/api/auth/google/callback"
            };
            return Challenge(properties, "Google");
        }

        // Step 2 — Google redirects back here after user approves
        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Read the claims Google sent back
            var result = await HttpContext
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                 return Redirect(
                    "http://localhost:4200/auth/login?error=google_failed");


            var claims = result.Principal!.Claims.ToList();

            var email = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email)?.Value ?? "";

            var fullName = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name)?.Value ?? "";

            var googleId = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";

            var avatarUrl = claims.FirstOrDefault(c =>
                c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(email))
                return Redirect(
                    "http://localhost:4200/auth/login?error=no_email");

            // Check if user already exists
            var existingUser = await _userRepository
                .FindByEmail(email);

            string token;

            if (existingUser != null)
            {
                // User exists — update provider info and login
                existingUser.Provider = "GOOGLE";
                if (avatarUrl != null)
                    existingUser.AvatarUrl = avatarUrl;
                token = _jwtHelper.GenerateToken(existingUser);
            }
            else
            {
                // New user — auto-register with Google info
                var newUser = new User
                {
                    Email = email,
                    FullName = fullName,
                    // Generate username from email
                    Username = await GenerateUniqueUsername(email),
                    PasswordHash = BCrypt.Net.BCrypt
                        .HashPassword(Guid.NewGuid().ToString()),
                    Role = "Developer",
                    Provider = "GOOGLE",
                    AvatarUrl = avatarUrl,
                    IsActive = true
                };

                token = await _authService.Register(newUser);
            }
            // Sign out of cookie — we use JWT from here
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect(
                $"http://localhost:4200/auth/oauth-callback" +
                $"?token={token}&provider=google");
        }

        // ─── GitHub OAuth2 ─────────────────────────────────────────────────

        // Step 1 — frontend redirects user here to start GitHub login
        [HttpGet("github/login")]
        public IActionResult GitHubLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/github/callback"
            };
            return Challenge(properties, "GitHub");
        }

        // Step 2 — GitHub redirects back here after user approves
        [HttpGet("github/callback")]
        public async Task<IActionResult> GitHubCallback()
        {
            var result = await HttpContext
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return Redirect(
                    "http://localhost:4200/auth/login?error=github_failed");

            var claims = result.Principal!.Claims.ToList();

            var email = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email)?.Value ?? "";

            var fullName = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name)?.Value ?? "";

            var githubUsername = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";

            var avatarUrl = claims.FirstOrDefault(c =>
                c.Type.Contains("avatar") ||
                c.Type.Contains("photo"))?.Value;

            // GitHub sometimes withholds email — fallback
            if (string.IsNullOrEmpty(email))
                email = $"{githubUsername}@github.local";

            var existingUser = await _userRepository
                .FindByEmail(email);

            string token;

            if (existingUser != null)
            {
                existingUser.Provider = "GITHUB";
                if (avatarUrl != null)
                    existingUser.AvatarUrl = avatarUrl;
                token = _jwtHelper.GenerateToken(existingUser);
            }
            else
            {
                var newUser = new User
                {
                    Email = email,
                    FullName = string.IsNullOrEmpty(fullName)
                        ? githubUsername : fullName,
                    Username = await GenerateUniqueUsername(
                        githubUsername),
                    PasswordHash = BCrypt.Net.BCrypt
                        .HashPassword(Guid.NewGuid().ToString()),
                    Role = "Developer",
                    Provider = "GITHUB",
                    AvatarUrl = avatarUrl,
                    IsActive = true
                };

                token = await _authService.Register(newUser);
            }
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect(
                $"http://localhost:4200/auth/oauth-callback" +
                $"?token={token}&provider=github");
        }

           

        // ─── Helper ────────────────────────────────────────────────────────

        // Generates a unique username from email/github handle
        // adds number suffix if username already taken
        private async Task<string> GenerateUniqueUsername(
            string baseInput)
        {
            var base64 = baseInput.Split('@')[0]
                .Replace("-", "_")
                .Replace(".", "_")
                .ToLower();

            var username = base64;
            var counter = 1;

            while (await _userRepository.ExistsByUsername(username))
            {
                username = $"{base64}{counter}";
                counter++;
            }

            return username;
        }
    }
}