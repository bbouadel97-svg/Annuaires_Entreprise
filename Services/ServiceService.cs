using System;
using System.Collections.Generic;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AnnuaireEntreprise.Services
{
    public class ServiceService
    {
        private Database _database;

        public ServiceService()
        {
            _database = new Database();
        }

        // Ajouter un service
        public void Ajouter(Service service)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute(@"
                INSERT INTO Services (Nom)
                VALUES (@Nom)
                ", service);
        }

        // Lister tous les services
        public List<Service> Lister()
        {
            using var connection = _database.GetConnection();
            connection.Open();

            return connection.Query<Service>("SELECT * FROM Services").ToList();
        }

        // Modifier un service
        public void Modifier(Service service)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute(@"
                UPDATE Services
                SET Nom = @Nom
                WHERE Id = @Id
                ", service);
        }

        // Supprimer un service
        public void Supprimer(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute("DELETE FROM Services WHERE Id = @Id", new { Id = id });
        }
    }
}