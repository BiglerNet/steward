using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class AssetPhotoConfiguration : IEntityTypeConfiguration<AssetPhoto>
{
    public void Configure(EntityTypeBuilder<AssetPhoto> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ThumbStorageKey).IsRequired();
        builder.Property(p => p.DisplayStorageKey).IsRequired();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.AssetId);
    }
}
