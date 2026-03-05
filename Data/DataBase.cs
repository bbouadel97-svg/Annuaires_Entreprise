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
    }
}