using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class MaintenanceItemConfiguration : IEntityTypeConfiguration<MaintenanceItem>
{
    public void Configure(EntityTypeBuilder<MaintenanceItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Engine>()
            .WithMany()
            .HasForeignKey(m => m.EngineId)
            .OnDelete(DeleteBehavior.Restrict);

        // TemplateId is a deliberately unconstrained historical pointer: deleting a Template
        // must not delete or null out MaintenanceItems that were created from it.
        builder.Property(m => m.TemplateId);

        builder.HasMany(m => m.ChecklistItems)
            .WithOne()
            .HasForeignKey(c => c.MaintenanceItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PartLines)
            .WithOne()
            .HasForeignKey(p => p.MaintenanceItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.AssetId);
    }
}
