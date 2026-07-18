using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Text).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>();

        builder.HasOne<Engine>()
            .WithMany()
            .HasForeignKey(c => c.EngineId)
            .OnDelete(DeleteBehavior.Restrict);

        // TemplateStepId is a deliberately unconstrained historical pointer, same reasoning as
        // MaintenanceItem.TemplateId: template steps may be edited/deleted independently of
        // checklist items already instantiated from them.
        builder.Property(c => c.TemplateStepId);

        builder.HasIndex(c => c.MaintenanceItemId);
    }
}
