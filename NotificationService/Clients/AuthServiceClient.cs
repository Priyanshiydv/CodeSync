using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NotificationService.Clients
{
    public interface IAuthServiceClient
    {
        Task<List<int>> GetAllUserIds();
        Task<List<int>> GetUserIdsByRole(string role);
        Task<int> GetUserIdByUsername(string username);
    }

    public class AuthServiceClient : IAuthServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private string? _cachedToken;

        public AuthServiceClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // Login as admin to get a JWT token for service-to-service calls
        private async Task<string> GetServiceToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken))
                return _cachedToken;

            var email = _config["ServiceAuth:AdminEmail"]!;
            var password = _config["ServiceAuth:AdminPassword"]!;

            var body = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "http://localhost:5157/api/auth/login", content);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _cachedToken = result?.Token ?? string.Empty;
            return _cachedToken;
        }

        private async Task<HttpRequestMessage> CreateRequest(
            HttpMethod method, string url)
        {
            var token = await GetServiceToken();
            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", token);
            return request;
        }

        public async Task<List<int>> GetAllUserIds()
        {
            var request = await CreateRequest(HttpMethod.Get,
                "http://localhost:5157/api/admin/users");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<int>();

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return users?.Select(u => u.UserId).ToList() ?? new List<int>();
        }

        public async Task<List<int>> GetUserIdsByRole(string role)
        {
            var allUsers = await GetAllUserIds();
            return allUsers;
        }

        public async Task<int> GetUserIdByUsername(string username)
        {
            var request = await CreateRequest(HttpMethod.Get,
                $"http://localhost:5157/api/auth/search?query={username}");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return 0;

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var match = users?.FirstOrDefault(u =>
                string.Equals(u.Username, username,
                    StringComparison.OrdinalIgnoreCase));

            return match?.UserId ?? 0;
        }

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }

        private class UserDto
        {
            public int UserId { get; set; }
            public string Role { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }
    }
}