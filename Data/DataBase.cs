using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace AnnuaireEntreprise.Data
{
    public class Database
    {
        private readonly string databasePath;

        public Database()
        {
            databasePath = ResolveDatabasePath();
        }

        private static string ResolveDatabasePath()
        {
            // During local development, prefer annuaire.db found by walking up from the app base folder.
            var currentPath = AppContext.BaseDirectory;
            for (var i = 0; i < 10; i++)
            {
                var candidate = Path.Combine(currentPath, "annuaire.db");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(currentPath);
                if (parent is null)
                {
                    break;
                }

                currentPath = parent.FullName;
            }

            var appDataPath = Path.Combine(FileSystem.Current.AppDataDirectory, "annuaire.db");
            var appDataDirectory = Path.GetDirectoryName(appDataPath);
            if (!string.IsNullOrWhiteSpace(appDataDirectory))
            {
                Directory.CreateDirectory(appDataDirectory);
            }

            return appDataPath;
        }

        private string ConnectionString
        {
            get
            {
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Cache = SqliteCacheMode.Shared,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    DefaultTimeout = 30,
                    Pooling = true
                };

                return builder.ToString();
            }
        }

        public string GetDatabasePath()
        {
            return databasePath;
        }

        public Microsoft.Data.Sqlite.SqliteConnection GetConnection()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString);
            connection.StateChange += (_, args) =>
            {
                if (args.CurrentState == ConnectionState.Open)
                {
                    using var pragma = connection.CreateCommand();
                    pragma.CommandText = @"
                    PRAGMA foreign_keys = ON;
                    PRAGMA busy_timeout = 5000;
                    ";
                    pragma.ExecuteNonQuery();
                }
            };

            return connection;
        }

        public void CreateTables()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Sites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Ville TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Services (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nom TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                PasswordSalt TEXT NOT NULL,
                Role TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Salaries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nom TEXT NOT NULL,
                Prenom TEXT NOT NULL,
                TelephoneFixe TEXT,
                TelephonePortable TEXT,
                Email TEXT,
                ServiceId INTEGER NOT NULL,
                SiteId INTEGER NOT NULL,
                FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE RESTRICT,
                FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE RESTRICT
            );
            ";

            command.ExecuteNonQuery();
        }

        // Ajouter des données test pour Sites et Services
        public void SeedData()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT OR IGNORE INTO Sites (Id, Ville) VALUES (1, 'Paris');
            INSERT OR IGNORE INTO Sites (Id, Ville) VALUES (2, 'Lyon');
            INSERT OR IGNORE INTO Services (Id, Nom) VALUES (1, 'Informatique');
            INSERT OR IGNORE INTO Services (Id, Nom) VALUES (2, 'Ressources Humaines');
            ";
            command.ExecuteNonQuery();

            var adminSalt = "annuaire-default-salt";
            var adminHash = ComputeHash("1997", adminSalt);

            var userCommand = connection.CreateCommand();
            userCommand.CommandText = @"
            INSERT OR IGNORE INTO Users (Username, PasswordHash, PasswordSalt, Role)
            VALUES ($username, $passwordHash, $passwordSalt, $role)
            ";
            userCommand.Parameters.AddWithValue("$username", "admin");
            userCommand.Parameters.AddWithValue("$passwordHash", adminHash);
            userCommand.Parameters.AddWithValue("$passwordSalt", adminSalt);
            userCommand.Parameters.AddWithValue("$role", "admin");
            userCommand.ExecuteNonQuery();
        }

        private static string ComputeHash(string password, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }
}