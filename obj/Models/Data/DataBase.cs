namespace AnnuaireEntreprise.obj.Models.Data
{
    public class DataBase
    {
        private string connectionString = "Data Source=annuaire.db";

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(connectionString);
        }
    }
}