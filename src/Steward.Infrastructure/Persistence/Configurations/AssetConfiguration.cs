using Steward.Domain.Entities;
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
            .HasValue<Vehicle>(nameof(Vehicle))
            .HasValue<Boat>(nameof(Boat))
            .HasValue<Trailer>(nameof(Trailer))
            .HasValue<Equipment>(nameof(Equipment));

        builder.HasIndex(a => a.HouseholdId);

        builder.HasOne<AssetPhoto>()
            .WithMany()
            .HasForeignKey(a => a.CoverPhotoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
