using System;
using System.Collections.Generic;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using Microsoft.Data.Sqlite;

namespace AnnuaireEntreprise.Services
{
    public class SiteService
    {
        private Database _database;

        public SiteService()
        {
            _database = new Database();
        }

        // Ajouter un site
        public void Ajouter(Site site)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Sites (Ville)
            VALUES ($ville)
            ";
            command.Parameters.AddWithValue("$ville", site.Ville);
            command.ExecuteNonQuery();
        }

        // Lister tous les sites
        public List<Site> Lister()
        {
            var result = new List<Site>();
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Sites";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Site
                {
                    Id = reader.GetInt32(0),
                    Ville = reader.GetString(1)
                });
            }

            return result;
        }

        // Modifier un site
        public void Modifier(Site site)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Sites
            SET Ville = $ville
            WHERE Id = $id
            ";
            command.Parameters.AddWithValue("$id", site.Id);
            command.Parameters.AddWithValue("$ville", site.Ville);
            command.ExecuteNonQuery();
        }

        // Supprimer un site
        public void Supprimer(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Sites WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
    }
}