using System.Security.Cryptography;
using System.Text;
using AnnuaireEntreprise.Data;
using Dapper;

namespace AnnuaireEntreprise.Services
{
    public class AuthService
    {
        private readonly Database _database;

        public AuthService()
        {
            _database = new Database();
        }

        public bool AuthenticateAdmin(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            using var connection = _database.GetConnection();
            connection.Open();

            var user = connection.QueryFirstOrDefault<UserRecord>(@"
                SELECT PasswordHash, PasswordSalt, Role
                FROM Users
                WHERE Username = @Username
                LIMIT 1
                ", new { Username = username.Trim() });

            if (user is null)
            {
                return false;
            }

            if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var computedHash = ComputeHash(password, user.PasswordSalt);
            return string.Equals(user.PasswordHash, computedHash, StringComparison.Ordinal);
        }

        private static string ComputeHash(string password, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        private sealed class UserRecord
        {
            public string PasswordHash { get; init; } = string.Empty;
            public string PasswordSalt { get; init; } = string.Empty;
            public string Role { get; init; } = string.Empty;
        }
    }
}
