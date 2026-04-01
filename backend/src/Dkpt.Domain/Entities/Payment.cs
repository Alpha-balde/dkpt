using Dkpt.Domain.Common;
using Dkpt.Domain.Enums;

namespace Dkpt.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid MemberId { get; set; }
    public int Annee { get; set; }
    public DateOnly DatePaiement { get; set; }
    public decimal Montant { get; set; }
    public decimal FraisPaiement { get; set; }
    public PaymentMethod MoyenPaiement { get; set; } = PaymentMethod.Especes;
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public Member Member { get; set; } = null!;
    public User? CreatedByUser { get; set; }
}
