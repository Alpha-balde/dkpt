using Dkpt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dkpt.Infrastructure.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(m => m.NumeroMembre).HasColumnName("numero_membre").IsRequired();
        builder.Property(m => m.Prenom).HasColumnName("prenom").IsRequired();
        builder.Property(m => m.Nom).HasColumnName("nom").IsRequired();
        builder.Property(m => m.Telephone).HasColumnName("telephone");
        builder.Property(m => m.WhatsApp).HasColumnName("whatsapp");
        builder.Property(m => m.Residence).HasColumnName("residence");
        builder.Property(m => m.Village).HasColumnName("village");
        builder.Property(m => m.SousPrefecture).HasColumnName("sous_prefecture_origine");
        builder.Property(m => m.NomCompletRaw).HasColumnName("nom_complet_raw");
        builder.Property(m => m.AnneeDebut).HasColumnName("annee_debut");
        builder.Property(m => m.Actif).HasColumnName("actif").HasDefaultValue(true);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(m => m.NumeroMembre).IsUnique();
    }
}
