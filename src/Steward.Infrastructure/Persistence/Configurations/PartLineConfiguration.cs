using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class PartLineConfiguration : IEntityTypeConfiguration<PartLine>
{
    public void Configure(EntityTypeBuilder<PartLine> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>();

        builder.HasOne<ChecklistItem>()
            .WithMany()
            .HasForeignKey(p => p.ChecklistItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Part>()
            .WithMany()
            .HasForeignKey(p => p.PartId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.MaintenanceItemId);
    }
}
