using Microsoft.Data.Sqlite;

namespace AnnuaireEntreprise.Data
{
    public class Database
    {
        private string connectionString = "Data Source=annuaire.db";

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(connectionString);
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