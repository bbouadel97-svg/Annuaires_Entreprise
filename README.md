# Annuaire Entreprise

## Objectif

Application lourde Windows en .NET MAUI pour consulter et administrer un annuaire d'entreprise.

## Choix techniques

- Interface graphique: .NET MAUI Windows avec XAML.
- Base relationnelle: SQLite locale.
- Acces aux donnees: SQL avec Dapper comme micro-ORM.
- Authentification: role administrateur stocke en base avec hash SHA256.
- Service tiers: RandomUser pour l'import d'utilisateurs.
- Generation PDF: QuestPDF pour produire des fiches salaries.
- Journalisation: fichier texte local pour les acces administrateur et les erreurs applicatives.

## Structure

- `Data/`: acces et initialisation de la base.
- `Models/`: entites metier.
- `Services/`: CRUD, authentification, API, PDF, logs.
- `MainPage.xaml` et `MainPage.xaml.cs`: interface et orchestration.

## Points de conformite

- Recherche visiteur par nom, site et service.
- Fiche detaillee salarie.
- Mode admin cache par `Ctrl + Shift + A`.
- CRUD pour les sites, services et salaries.
- Import API publique.
- Export PDF via bibliotheque externe.
- Logs texte pour les acces admin et les erreurs.