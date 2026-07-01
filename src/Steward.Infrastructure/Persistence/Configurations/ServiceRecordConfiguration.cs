using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRecord> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Description).IsRequired();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(s => s.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Engine>()
            .WithMany()
            .HasForeignKey(s => s.EngineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.AssetId);
    }
}
