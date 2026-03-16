using Microsoft.Data.Sqlite;
using Microsoft.Maui.Storage;

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

        private string ConnectionString => $"Data Source={databasePath}";

        public string GetDatabasePath()
        {
            return databasePath;
        }

        public Microsoft.Data.Sqlite.SqliteConnection GetConnection()
        {
            return new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString);
        }

        public void CreateTables()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Sites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Ville TEXT
            );

            CREATE TABLE IF NOT EXISTS Services (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nom TEXT
            );

            CREATE TABLE IF NOT EXISTS Salaries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nom TEXT,
                Prenom TEXT,
                TelephoneFixe TEXT,
                TelephonePortable TEXT,
                Email TEXT,
                ServiceId INTEGER,
                SiteId INTEGER
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
        }
    }
}