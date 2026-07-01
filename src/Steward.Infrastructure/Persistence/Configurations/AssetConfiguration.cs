using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired();

        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<Snowmobile>(nameof(Snowmobile))
            .HasValue<Utv>(nameof(Utv))
            .HasValue<Boat>(nameof(Boat))
            .HasValue<Car>(nameof(Car))
            .HasValue<Truck>(nameof(Truck))
            .HasValue<SnowmobileTrailer>(nameof(SnowmobileTrailer))
            .HasValue<EnclosedTrailer>(nameof(EnclosedTrailer))
            .HasValue<RidingMower>(nameof(RidingMower))
            .HasValue<PowerWasher>(nameof(PowerWasher))
            .HasValue<SmallEngine>(nameof(SmallEngine));

        builder.HasIndex(a => a.HouseholdId);
    }
}
