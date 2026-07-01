using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class FuelLogConfiguration : IEntityTypeConfiguration<FuelLog>
{
    public void Configure(EntityTypeBuilder<FuelLog> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(f => f.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Engine>()
            .WithMany()
            .HasForeignKey(f => f.EngineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.AssetId);
    }
}
