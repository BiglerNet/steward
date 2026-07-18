using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class TemplateStepSuggestedPartConfiguration : IEntityTypeConfiguration<TemplateStepSuggestedPart>
{
    public void Configure(EntityTypeBuilder<TemplateStepSuggestedPart> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired();

        builder.HasIndex(p => p.TemplateStepId);
    }
}
