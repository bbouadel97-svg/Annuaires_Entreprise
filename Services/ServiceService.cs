using System;
using System.Collections.Generic;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
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

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Services (Nom)
            VALUES ($nom)
            ";
            command.Parameters.AddWithValue("$nom", service.Nom);
            command.ExecuteNonQuery();
        }

        // Lister tous les services
        public List<Service> Lister()
        {
            var result = new List<Service>();
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Services";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Service
                {
                    Id = reader.GetInt32(0),
                    Nom = reader.GetString(1)
                });
            }

            return result;
        }

        // Modifier un service
        public void Modifier(Service service)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Services
            SET Nom = $nom
            WHERE Id = $id
            ";
            command.Parameters.AddWithValue("$id", service.Id);
            command.Parameters.AddWithValue("$nom", service.Nom);
            command.ExecuteNonQuery();
        }

        // Supprimer un service
        public void Supprimer(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Services WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
    }
}