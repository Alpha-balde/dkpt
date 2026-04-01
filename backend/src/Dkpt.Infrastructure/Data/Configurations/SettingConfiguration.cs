using Dkpt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dkpt.Infrastructure.Data.Configurations;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("settings", t =>
        {
            t.HasCheckConstraint("settings_id_check", "\"id\" = 1");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValue(1);
        builder.Property(s => s.MontantCotisationAnnuelleParDefaut)
            .HasColumnName("montant_cotisation_annuelle_par_defaut")
            .HasColumnType("numeric")
            .HasDefaultValue(50000);
    }
}
