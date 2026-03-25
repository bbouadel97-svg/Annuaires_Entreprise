using System.Security.Cryptography;
using System.Text;
using AnnuaireEntreprise.Data;

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

            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT PasswordHash, PasswordSalt, Role
            FROM Users
            WHERE Username = $username
            LIMIT 1
            ";
            command.Parameters.AddWithValue("$username", username.Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return false;
            }

            var storedHash = reader.GetString(0);
            var storedSalt = reader.GetString(1);
            var role = reader.GetString(2);

            if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var computedHash = ComputeHash(password, storedSalt);
            return string.Equals(storedHash, computedHash, StringComparison.Ordinal);
        }

        private static string ComputeHash(string password, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }
}
