using Dkpt.Domain.Entities;
using Dkpt.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dkpt.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.MemberId).HasColumnName("member_id").IsRequired();
        builder.Property(p => p.Annee).HasColumnName("annee").IsRequired();
        builder.Property(p => p.DatePaiement).HasColumnName("date_paiement").IsRequired();
        builder.Property(p => p.Montant).HasColumnName("montant").HasColumnType("numeric").IsRequired();
        builder.Property(p => p.FraisPaiement).HasColumnName("frais_paiement").HasColumnType("numeric").HasDefaultValue(0);
        builder.Property(p => p.MoyenPaiement).HasColumnName("moyen_paiement")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(p => p.Reference).HasColumnName("reference");
        builder.Property(p => p.Note).HasColumnName("note");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasOne(p => p.Member)
            .WithMany(m => m.Payments)
            .HasForeignKey(p => p.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
