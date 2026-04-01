using Dkpt.Domain.Common;

namespace Dkpt.Domain.Entities;

public class Member : BaseEntity
{
    public string NumeroMembre { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Residence { get; set; }
    public string? Village { get; set; }
    public string? SousPrefecture { get; set; }
    public string? NomCompletRaw { get; set; }
    public int AnneeDebut { get; set; } = DateTime.UtcNow.Year;
    public bool Actif { get; set; } = true;

    // Navigation
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
