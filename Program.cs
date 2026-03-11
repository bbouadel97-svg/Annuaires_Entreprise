using System;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using AnnuaireEntreprise.Services;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Database db = new Database();
        db.CreateTables(); // Assure-toi que les tables existent
        db.SeedData(); // <-- ça va créer Sites et Services de test

        SalarieService service = new SalarieService();

        bool continuer = true;

        while (continuer)
        {
            Console.WriteLine("\n=== Menu Annuaire Entreprise ===");
            Console.WriteLine("1 - Lister tous les salariés");
            Console.WriteLine("2 - Ajouter un salarié");
            Console.WriteLine("3 - Modifier un salarié");
            Console.WriteLine("4 - Supprimer un salarié");
            Console.WriteLine("5 - Rechercher un salarié par nom");
            Console.WriteLine("6 - Quitter");
            Console.Write("Choix : ");

            string choix = Console.ReadLine() ?? string.Empty;
            switch (choix)
            {
                case "1":
                    ListerSalaries(service);
                    break;
                case "2":
                    AjouterSalarie(service);
                    break;
                case "3":
                    ModifierSalarie(service);
                    break;
                case "4":
                    SupprimerSalarie(service);
                    break;
                case "5":
                    RechercherSalarie(service);
                    break;
                case "6":
                    continuer = false;
                    break;
                default:
                    Console.WriteLine("Choix invalide !");
                    break;
            }
        }
    }

    static void ListerSalaries(SalarieService service)
    {
        var liste = service.Lister();
        Console.WriteLine("\n--- Liste des salariés ---");
        foreach (var s in liste)
        {
            Console.WriteLine($"{s.Id} - {s.Nom} {s.Prenom} - Email: {s.Email}");
        }
    }

    static void AjouterSalarie(SalarieService service)
    {
        Console.Write("Nom : ");
        string nom = Console.ReadLine() ?? string.Empty;
        Console.Write("Prénom : ");
        string prenom = Console.ReadLine() ?? string.Empty;
        Console.Write("Téléphone fixe : ");
        string fixe = Console.ReadLine() ?? string.Empty;
        Console.Write("Téléphone portable : ");
        string portable = Console.ReadLine() ?? string.Empty;
        Console.Write("Email : ");
        string email = Console.ReadLine() ?? string.Empty;
        Console.Write("ServiceId : ");
        int serviceId = int.Parse(Console.ReadLine() ?? "0");
        Console.Write("SiteId : ");
        int siteId = int.Parse(Console.ReadLine() ?? "0");

        service.Ajouter(new Salarie
        {
            Nom = nom,
            Prenom = prenom,
            TelephoneFixe = fixe,
            TelephonePortable = portable,
            Email = email,
            ServiceId = serviceId,
            SiteId = siteId
        });

        Console.WriteLine("Salarié ajouté !");
    }

    static void ModifierSalarie(SalarieService service)
    {
        Console.Write("Id du salarié à modifier : ");
        int id = int.Parse(Console.ReadLine() ?? "0");

        Console.Write("Nouveau nom : ");
        string nom = Console.ReadLine() ?? string.Empty;
        Console.Write("Nouveau prénom : ");
        string prenom = Console.ReadLine() ?? string.Empty;
        Console.Write("Nouveau téléphone fixe : ");
        string fixe = Console.ReadLine() ?? string.Empty;
        Console.Write("Nouveau téléphone portable : ");
        string portable = Console.ReadLine() ?? string.Empty;
        Console.Write("Nouvel email : ");
        string email = Console.ReadLine() ?? string.Empty;
        Console.Write("Nouveau ServiceId : ");
        int serviceId = int.Parse(Console.ReadLine() ?? "0");
        Console.Write("Nouveau SiteId : ");
        int siteId = int.Parse(Console.ReadLine() ?? "0");

        service.Modifier(new Salarie
        {
            Id = id,
            Nom = nom,
            Prenom = prenom,
            TelephoneFixe = fixe,
            TelephonePortable = portable,
            Email = email,
            ServiceId = serviceId,
            SiteId = siteId
        });

        Console.WriteLine("Salarié modifié !");
    }

    static void SupprimerSalarie(SalarieService service)
    {
        Console.Write("Id du salarié à supprimer : ");
        int id = int.Parse(Console.ReadLine() ?? "0");
        service.Supprimer(id);
        Console.WriteLine("Salarié supprimé !");
    }

    static void RechercherSalarie(SalarieService service)
    {
        Console.Write("Nom à rechercher : ");
        string nom = Console.ReadLine() ?? string.Empty;
        var liste = service.Lister();
        var resultat = liste.FindAll(s => s.Nom.Contains(nom, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine("\n--- Résultat recherche ---");
        foreach (var s in resultat)
        {
            Console.WriteLine($"{s.Id} - {s.Nom} {s.Prenom} - Email: {s.Email}");
        }
        if (resultat.Count == 0) Console.WriteLine("Aucun salarié trouvé.");
    }
}
