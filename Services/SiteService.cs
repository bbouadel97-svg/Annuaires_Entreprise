using System;
using System.Collections.Generic;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using Dapper;
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

            connection.Execute(@"
                INSERT INTO Sites (Ville)
                VALUES (@Ville)
                ", site);
        }

        // Lister tous les sites
        public List<Site> Lister()
        {
            using var connection = _database.GetConnection();
            connection.Open();

            return connection.Query<Site>("SELECT * FROM Sites").ToList();
        }

        // Modifier un site
        public void Modifier(Site site)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute(@"
                UPDATE Sites
                SET Ville = @Ville
                WHERE Id = @Id
                ", site);
        }

        // Supprimer un site
        public void Supprimer(int id)
        {
            using var connection = _database.GetConnection();
            connection.Open();

            connection.Execute("DELETE FROM Sites WHERE Id = @Id", new { Id = id });
        }
    }
}