namespace AnnuaireEntreprise.Models
{
    public class Salarie
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string TelephoneFixe { get; set; } = string.Empty;
        public string TelephonePortable { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int ServiceId { get; set; }
        public int SiteId { get; set; }
    }
}
