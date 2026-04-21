using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using Dapper;

namespace AnnuaireEntreprise.Services
{
    public class SalarieService
    {
        private Database _database;

        public SalarieService()
        {
            _database = new Database();
        }

        // Ajouter un salarié
        public void Ajouter(Salarie salarie)   
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute(@"
                INSERT INTO Salaries (Nom, Prenom, TelephoneFixe, TelephonePortable, Email, ServiceId, SiteId)
                VALUES (@Nom, @Prenom, @TelephoneFixe, @TelephonePortable, @Email, @ServiceId, @SiteId)
                ", salarie);
        }

        // Lister tous les salariés
        public List<Salarie> Lister()
        {
            using var connection = _database.GetConnection();
            connection.Open();

            return connection.Query<Salarie>("SELECT * FROM Salaries").ToList();
        }

        // Supprimer un salarié par Id
        public void Supprimer(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute("DELETE FROM Salaries WHERE Id = @Id", new { Id = id });
        }

        // Modifier un salarié
        public void Modifier(Salarie salarie)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute(@"
                UPDATE Salaries
                SET Nom = @Nom,
                    Prenom = @Prenom,
                    TelephoneFixe = @TelephoneFixe,
                    TelephonePortable = @TelephonePortable,
                    Email = @Email,
                    ServiceId = @ServiceId,
                    SiteId = @SiteId
                WHERE Id = @Id
                ", salarie);
        }
    }
}
