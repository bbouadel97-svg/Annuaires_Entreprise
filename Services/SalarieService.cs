using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;

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

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Salaries (Nom, Prenom, TelephoneFixe, TelephonePortable, Email, ServiceId, SiteId)
            VALUES ($nom, $prenom, $fixe, $portable, $email, $serviceId, $siteId)
            ";
            command.Parameters.AddWithValue("$nom", salarie.Nom);
            command.Parameters.AddWithValue("$prenom", salarie.Prenom);
            command.Parameters.AddWithValue("$fixe", salarie.TelephoneFixe);
            command.Parameters.AddWithValue("$portable", salarie.TelephonePortable);
            command.Parameters.AddWithValue("$email", salarie.Email);
            command.Parameters.AddWithValue("$serviceId", salarie.ServiceId);
            command.Parameters.AddWithValue("$siteId", salarie.SiteId);

            command.ExecuteNonQuery();
        }

        // Lister tous les salariés
        public List<Salarie> Lister()
        {
            List<Salarie> result = new List<Salarie>();

            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Salaries";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Salarie
                {
                    Id = reader.GetInt32(0),
                    Nom = reader.GetString(1),
                    Prenom = reader.GetString(2),
                    TelephoneFixe = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    TelephonePortable = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    ServiceId = reader.GetInt32(6),
                    SiteId = reader.GetInt32(7)
                });
            }

            return result;
        }

        // Supprimer un salarié par Id
        public void Supprimer(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Salaries WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        // Modifier un salarié
        public void Modifier(Salarie salarie)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Salaries
            SET Nom = $nom,
                Prenom = $prenom,
                TelephoneFixe = $fixe,
                TelephonePortable = $portable,
                Email = $email,
                ServiceId = $serviceId,
                SiteId = $siteId
            WHERE Id = $id
            ";
            command.Parameters.AddWithValue("$id", salarie.Id);
            command.Parameters.AddWithValue("$nom", salarie.Nom);
            command.Parameters.AddWithValue("$prenom", salarie.Prenom);
            command.Parameters.AddWithValue("$fixe", salarie.TelephoneFixe);
            command.Parameters.AddWithValue("$portable", salarie.TelephonePortable);
            command.Parameters.AddWithValue("$email", salarie.Email);
            command.Parameters.AddWithValue("$serviceId", salarie.ServiceId);
            command.Parameters.AddWithValue("$siteId", salarie.SiteId);

            command.ExecuteNonQuery();
        }
    }
}