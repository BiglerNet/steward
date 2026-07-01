using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class EngineConfiguration : IEntityTypeConfiguration<Engine>
{
    public void Configure(EntityTypeBuilder<Engine> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Label).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();

        builder.Property(e => e.HorsepowerHp).HasColumnType("decimal(8,2)");
        builder.Property(e => e.TorqueNm).HasColumnType("decimal(8,2)");
        builder.Property(e => e.OilCapacityL).HasColumnType("decimal(6,3)");
        builder.Property(e => e.CoolantCapacityL).HasColumnType("decimal(6,3)");

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(e => e.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.AssetId);
    }
}
