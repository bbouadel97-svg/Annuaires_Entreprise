// See https://aka.ms/new-console-template for more information
using System;
using AnnuaireEntreprise.Data;

class Program
{
    static void Main()
    {
        Database db = new Database();

        db.CreateTables();

        Console.WriteLine("Tables créées !");
    }
}
