using System.Net.Http;
using System.Text.Json;

namespace NotificationService.Clients
{
    public interface IAuthServiceClient
    {
        Task<List<int>> GetAllUserIds();
        Task<List<int>> GetUserIdsByRole(string role);
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

        private class UserDto
        {
            public int UserId { get; set; }
            public string Role { get; set; } = string.Empty;
        }
    }
}