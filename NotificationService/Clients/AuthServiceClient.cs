using System.Net.Http;
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

        public AuthServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<int>> GetAllUserIds()
        {
            var response = await _httpClient.GetAsync("http://localhost:5157/api/admin/users");
            if (!response.IsSuccessStatusCode)
                return new List<int>();

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return users?.Select(u => u.UserId).ToList() ?? new List<int>();
        }

        public async Task<List<int>> GetUserIdsByRole(string role)
        {
            var allUsers = await GetAllUserIds(); // Simplify: get all and filter, or add role endpoint
            return allUsers;
        }

        // used by SendMentionNotification to resolve @username → userId
            public async Task<int> GetUserIdByUsername(string username)
            {
                var response = await _httpClient
                    .GetAsync(
                        $"http://localhost:5157/api/auth/search?query={username}");

                if (!response.IsSuccessStatusCode)
                    return 0;

                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDto>>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                // Find exact username match (case-insensitive)
                var match = users?.FirstOrDefault(u =>
                    string.Equals(u.Username, username,
                        StringComparison.OrdinalIgnoreCase));

                return match?.UserId ?? 0;
            }

            private class UserDto
            {
                public int UserId { get; set; }
                public string Role { get; set; } = string.Empty;
                // ADD — needed for username resolution
                public string Username { get; set; } = string.Empty;
        
            }
    }
}