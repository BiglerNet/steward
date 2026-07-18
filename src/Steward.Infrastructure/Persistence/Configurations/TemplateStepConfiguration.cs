using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class TemplateStepConfiguration : IEntityTypeConfiguration<TemplateStep>
{
    public void Configure(EntityTypeBuilder<TemplateStep> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Text).IsRequired();

        builder.HasMany(s => s.SuggestedParts)
            .WithOne()
            .HasForeignKey(p => p.TemplateStepId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.TemplateId);
    }
}
