using Dkpt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dkpt.Infrastructure.Data.Configurations;

public class ContributionAmountConfiguration : IEntityTypeConfiguration<ContributionAmount>
{
    public void Configure(EntityTypeBuilder<ContributionAmount> builder)
    {
        builder.ToTable("contribution_amounts", t =>
        {
            t.HasCheckConstraint("chk_contribution_amount_positive", "\"amount\" > 0");
            t.HasCheckConstraint("chk_contribution_year_min", "\"year\" >= 2022");
        });

        builder.HasKey(c => c.Year);
        builder.Property(c => c.Year).HasColumnName("year").ValueGeneratedNever();
        builder.Property(c => c.Amount).HasColumnName("amount").HasDefaultValue(60000);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
    }
}
