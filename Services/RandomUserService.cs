using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnnuaireEntreprise.Services
{
    public class RandomUserService
    {
        private readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<RandomUserResponse?> GetRandomUsersAsync(int results = 10)
        {
            if (results <= 0)
            {
                results = 1;
            }

            var url = $"https://randomuser.me/api/?results={results}&nat=fr";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                return JsonSerializer.Deserialize<RandomUserResponse>(response, JsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }

    // 🔽 Classes pour lire le JSON
    public class RandomUserResponse
    {
        public Result[] Results { get; set; } = [];
    }

    public class Result
    {
        public Name Name { get; set; } = new();
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Cell { get; set; } = string.Empty;
    }

    public class Name
    {
        public string First { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;
    }
}